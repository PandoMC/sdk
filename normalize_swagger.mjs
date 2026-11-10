// Normalizes the OpenAPI document published by the Partner API before it is fed to Kiota.
//
// The API publishes OpenAPI 3.1 (Microsoft.AspNetCore.OpenApi). Kiota turns several 3.1 idioms into awkward
// types (e.g. `type: [number, string]` becomes UntypedNode, `oneOf: [null, $ref]` becomes a wrapper class),
// and some details present in the earlier Swashbuckle output are no longer emitted. This script rewrites the
// document so the generated SDKs keep the same, strongly typed public surface.
//
// Usage: node normalize_swagger.mjs <swagger.json>   (file is rewritten in place)

import { readFileSync, writeFileSync } from "node:fs";

const file = process.argv[2];
if (!file) throw new Error("Usage: node normalize_swagger.mjs <swagger.json>");

const doc = JSON.parse(readFileSync(file, "utf8").replace(/^﻿/, ""));
const schemas = doc.components.schemas;
const schemaRef = (name) => `#/components/schemas/${name}`;

// 1. Keep the list result names used by earlier SDK versions: ListResultOfOrder -> OrderListResult.
const renames = {};
for (const name of Object.keys(schemas)) {
  const match = /^ListResultOf(\w+)$/.exec(name);
  if (match) renames[schemaRef(name)] = schemaRef(`${match[1]}ListResult`);
}
for (const [from, to] of Object.entries(renames)) {
  const fromName = from.split("/").pop();
  schemas[to.split("/").pop()] = schemas[fromName];
  delete schemas[fromName];
}

// Scalar component schemas (e.g. OrderStatus: { type: integer }) are inlined, as before.
const isScalarSchema = (s) => s && !s.properties && typeof s.type === "string" && s.type !== "object" && s.type !== "array";

function normalize(node) {
  if (Array.isArray(node)) return node.map(normalize);
  if (!node || typeof node !== "object") return node;

  let result = {};
  for (const [key, value] of Object.entries(node)) result[key] = normalize(value);

  if (typeof result.$ref === "string" && renames[result.$ref]) result.$ref = renames[result.$ref];

  // 2. Nullable references: oneOf: [{ type: null }, { $ref }] -> { $ref }
  for (const keyword of ["oneOf", "anyOf"]) {
    const members = result[keyword];
    if (!Array.isArray(members)) continue;
    const nonNull = members.filter((m) => m.type !== "null");
    if (nonNull.length === 1 && nonNull.length < members.length) {
      const { [keyword]: _, ...rest } = result;
      result = { ...rest, ...nonNull[0] };
    }
  }

  // 3. Inline scalar component schemas so they keep generating as plain int/string properties.
  if (typeof result.$ref === "string") {
    const target = schemas[result.$ref.split("/").pop()];
    if (isScalarSchema(target)) {
      const { $ref: _, ...siblings } = result;
      result = { ...normalize(target), ...siblings };
    }
  }

  // 4. Type unions: ["null", "integer", "string"] -> "integer" (numbers are also allowed as strings in 3.1 output)
  if (Array.isArray(result.type)) {
    let types = result.type.filter((t) => t !== "null");
    const numeric = types.find((t) => t === "integer" || t === "number");
    if (numeric && types.includes("string")) {
      types = types.filter((t) => t !== "string");
      delete result.pattern;
    }
    result.type = types.length === 1 ? types[0] : types;
  }
  if (result.type === "integer" && !result.format) result.format = "int32";

  return result;
}

doc.paths = normalize(doc.paths);
for (const name of Object.keys(schemas)) schemas[name] = normalize(schemas[name]);

// Drop the now inlined scalar schemas.
for (const name of Object.keys(schemas)) if (isScalarSchema(schemas[name])) delete schemas[name];

// 5. Models are closed, except ProblemDetails which may carry extensions (as before).
for (const [name, schema] of Object.entries(schemas)) {
  if (schema.type === "object" && schema.additionalProperties === undefined)
    schema.additionalProperties = name === "ProblemDetails" ? {} : false;
}

// 6. Error responses are no longer described; the API still returns ProblemDetails (RFC 7807) for these.
schemas.ProblemDetails ??= {
  type: "object",
  properties: {
    type: { type: "string" },
    title: { type: "string" },
    status: { type: "integer", format: "int32" },
    detail: { type: "string" },
    instance: { type: "string" },
  },
  additionalProperties: {},
};
const problem = (description) => ({
  description,
  content: { "application/json": { schema: { $ref: schemaRef("ProblemDetails") } } },
});
for (const operations of Object.values(doc.paths)) {
  for (const operation of Object.values(operations)) {
    operation.responses["400"] ??= problem("Bad Request");
    operation.responses["500"] ??= problem("Internal Server Error");
  }
}

// 7. Keep the sort directions of earlier SDK versions: asc/desc -> ASC/DESC. The API accepts either casing.
for (const operations of Object.values(doc.paths)) {
  for (const operation of Object.values(operations)) {
    const schema = operation.parameters?.find((p) => p.name === "sortDirection")?.schema;
    if (!schema) continue;
    if (Array.isArray(schema.enum)) schema.enum = schema.enum.map((value) => String(value).toUpperCase());
    if (typeof schema.default === "string") schema.default = schema.default.toUpperCase();
  }
}

writeFileSync(file, JSON.stringify(doc, null, 2) + "\n", "utf8");
