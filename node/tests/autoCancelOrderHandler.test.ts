import { describe, it, expect, beforeEach } from "vitest";
import type { RequestOption } from "@microsoft/kiota-abstractions";
import type { Middleware } from "@microsoft/kiota-http-fetchlibrary";
import { AutoCancelOrderHandler } from "../src/client/autoCancelOrderHandler";
import { MissionControlHeaders } from "../src/client/missionControlHeaders";

const TEST_REQUEST_ID = "aaaabbbb-cccc-dddd-eeee-ffffffffffff";
const BASE = "https://api.test.com";

// -- Recording middleware (equivalent to C# RecordingHandler) ----------------

interface CapturedRequest {
  url: string;
  init: RequestInit;
  body: string | null;
}

class RecordingMiddleware implements Middleware {
  next: Middleware | undefined;
  sentRequests: CapturedRequest[] = [];
  private queue: Array<Response | Error> = [];

  enqueueResponse(status: number, body?: string): void {
    this.queue.push(new Response(body ?? null, { status }));
  }

  enqueueError(error: Error): void {
    this.queue.push(error);
  }

  async execute(
    url: string,
    requestInit: RequestInit,
    _requestOptions?: Record<string, RequestOption>,
  ): Promise<Response> {
    const body = requestInit.body != null ? String(requestInit.body) : null;
    this.sentRequests.push({ url, init: { ...requestInit }, body });

    const item = this.queue.shift();
    if (!item) return new Response(null, { status: 200 });
    if (item instanceof Error) throw item;
    return item;
  }
}

// -- Helpers -----------------------------------------------------------------

function createPipeline(): {
  handler: AutoCancelOrderHandler;
  inner: RecordingMiddleware;
} {
  const inner = new RecordingMiddleware();
  const handler = new AutoCancelOrderHandler();
  handler.next = inner;
  return { handler, inner };
}

function createOrderBody(requestId: string): string {
  return JSON.stringify({ requestId });
}

function createOrderInit(path: string, requestId: string): [string, RequestInit] {
  return [
    `${BASE}${path}`,
    {
      method: "POST",
      headers: new Headers({ "Content-Type": "application/json" }),
      body: createOrderBody(requestId),
    },
  ];
}

function assertIsCancelRequest(
  captured: CapturedRequest,
  expectedRequestId: string,
): void {
  expect(new URL(captured.url).pathname).toContain("/Order/cancelOrder");
  expect(captured.body).not.toBeNull();
  const parsed = JSON.parse(captured.body!);
  expect(parsed.originalRequestId).toBe(expectedRequestId);
}

// -- Tests -------------------------------------------------------------------

describe("AutoCancelOrderHandler", () => {
  let handler: AutoCancelOrderHandler;
  let inner: RecordingMiddleware;

  beforeEach(() => {
    ({ handler, inner } = createPipeline());
  });

  it("successful request does not send cancel request", async () => {
    inner.enqueueResponse(200);
    const [url, init] = createOrderInit("/Order/createOrder", TEST_REQUEST_ID);

    const response = await handler.execute(url, init);

    expect(response.status).toBe(200);
    expect(inner.sentRequests).toHaveLength(1);
  });

  it.each([500, 502, 503, 504])(
    "server error %i sends cancel request",
    async (status) => {
      inner.enqueueResponse(status);
      inner.enqueueResponse(200); // cancel response
      const [url, init] = createOrderInit(
        "/Order/createOrder",
        TEST_REQUEST_ID,
      );

      const response = await handler.execute(url, init);

      expect(response.status).toBe(status);
      expect(inner.sentRequests).toHaveLength(2);
      assertIsCancelRequest(inner.sentRequests[1], TEST_REQUEST_ID);
    },
  );

  it("client error does not send cancel request", async () => {
    inner.enqueueResponse(400);
    const [url, init] = createOrderInit("/Order/createOrder", TEST_REQUEST_ID);

    const response = await handler.execute(url, init);

    expect(response.status).toBe(400);
    expect(inner.sentRequests).toHaveLength(1);
  });

  it("connection error sends cancel request and rethrows", async () => {
    inner.enqueueError(new TypeError("fetch failed"));
    inner.enqueueResponse(200); // cancel response
    const [url, init] = createOrderInit("/Order/createOrder", TEST_REQUEST_ID);

    await expect(handler.execute(url, init)).rejects.toThrow("fetch failed");

    expect(inner.sentRequests).toHaveLength(2);
    assertIsCancelRequest(inner.sentRequests[1], TEST_REQUEST_ID);
  });

  it("non-order endpoint does not send cancel request on error", async () => {
    inner.enqueueResponse(500);
    const response = await handler.execute(`${BASE}/Product`, {
      method: "POST",
      headers: new Headers({ "Content-Type": "application/json" }),
      body: "{}",
    });

    expect(response.status).toBe(500);
    expect(inner.sentRequests).toHaveLength(1);
  });

  it("claimReservation sends cancel request on error", async () => {
    inner.enqueueResponse(500);
    inner.enqueueResponse(200);
    const [url, init] = createOrderInit(
      "/Order/claimReservation/some-id",
      TEST_REQUEST_ID,
    );

    await handler.execute(url, init);

    expect(inner.sentRequests).toHaveLength(2);
    assertIsCancelRequest(inner.sentRequests[1], TEST_REQUEST_ID);
  });

  it("reportOrder sends cancel request on error", async () => {
    inner.enqueueResponse(500);
    inner.enqueueResponse(200);
    const [url, init] = createOrderInit(
      "/Order/reportOrder",
      TEST_REQUEST_ID,
    );

    await handler.execute(url, init);

    expect(inner.sentRequests).toHaveLength(2);
    assertIsCancelRequest(inner.sentRequests[1], TEST_REQUEST_ID);
  });

  it("missing requestId does not send cancel request", async () => {
    inner.enqueueResponse(500);

    const response = await handler.execute(`${BASE}/Order/createOrder`, {
      method: "POST",
      headers: new Headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({ productId: "aaaabbbb-cccc-dddd-eeee-ffffffffffff" }),
    });

    expect(response.status).toBe(500);
    expect(inner.sentRequests).toHaveLength(1);
  });

  it("cancel request fails — original error still returned", async () => {
    inner.enqueueResponse(500); // original 5xx
    inner.enqueueError(new Error("Cancel failed")); // cancel blows up
    const [url, init] = createOrderInit("/Order/createOrder", TEST_REQUEST_ID);

    const response = await handler.execute(url, init);

    expect(response.status).toBe(500);
  });

  it("copies auth and partner-id headers to cancel request", async () => {
    inner.enqueueResponse(500);
    inner.enqueueResponse(200);

    const headers = new Headers({
      "Content-Type": "application/json",
      Authorization: "Bearer test-token",
      [MissionControlHeaders.PartnerId]: "partner-123",
    });

    const response = await handler.execute(`${BASE}/Order/createOrder`, {
      method: "POST",
      headers,
      body: createOrderBody(TEST_REQUEST_ID),
    });

    expect(response.status).toBe(500);
    expect(inner.sentRequests).toHaveLength(2);

    const cancelInit = inner.sentRequests[1].init;
    const cancelHeaders = new Headers(cancelInit.headers);
    expect(cancelHeaders.get("Authorization")).toBe("Bearer test-token");
    expect(cancelHeaders.get(MissionControlHeaders.PartnerId)).toBe(
      "partner-123",
    );
  });

  it("cancel URI preserves path prefix", async () => {
    inner.enqueueResponse(500);
    inner.enqueueResponse(200);
    const [url, init] = createOrderInit(
      "/partnerApi/v2/Order/createOrder",
      TEST_REQUEST_ID,
    );

    await handler.execute(url, init);

    const cancelUrl = new URL(inner.sentRequests[1].url);
    expect(cancelUrl.pathname).toBe("/partnerApi/v2/Order/cancelOrder");
  });

  it("cancel URI strips query string", async () => {
    inner.enqueueResponse(500);
    inner.enqueueResponse(200);

    const headers = new Headers({ "Content-Type": "application/json" });
    await handler.execute(
      `${BASE}/Order/createOrder?foo=bar`,
      {
        method: "POST",
        headers,
        body: createOrderBody(TEST_REQUEST_ID),
      },
    );

    const cancelUrl = new URL(inner.sentRequests[1].url);
    expect(cancelUrl.search).toBe("");
  });
});
