import type { RequestOption } from "@microsoft/kiota-abstractions";
import type { Middleware } from "@microsoft/kiota-http-fetchlibrary";
import { MissionControlHeaders } from "./missionControlHeaders";

const AUTO_CANCEL_PATH_PREFIXES = [
  "/Order/createOrder",
  "/Order/claimReservation",
  "/Order/reportOrder",
] as const;

const CANCEL_TIMEOUT_MS = 30_000;

/**
 * A Kiota middleware that automatically attempts to cancel an order when a request to one of
 * the order-mutation endpoints fails with a server error (5xx) or a network/connection error.
 *
 * Affected endpoints: `createOrder`, `claimReservation`, `reportOrder`.
 *
 * The cancellation attempt is best-effort. If it fails the original error is still propagated
 * to the caller unchanged.
 */
export class AutoCancelOrderHandler implements Middleware {
  next: Middleware | undefined;

  async execute(
    url: string,
    requestInit: RequestInit,
    requestOptions?: Record<string, RequestOption>,
  ): Promise<Response> {
    if (!isAutoCancelEndpoint(url)) {
      return this.next!.execute(url, requestInit, requestOptions);
    }

    // Buffer the request body before the inner handler consumes it.
    const requestBody =
      requestInit.body != null ? String(requestInit.body) : null;

    let response: Response;
    try {
      response = await this.next!.execute(url, requestInit, requestOptions);
    } catch (err) {
      // Network/connection error — attempt a best-effort cancel.
      await this.tryCancelOrderAsync(url, requestInit, requestBody);
      throw err;
    }

    if (isServerError(response)) {
      await this.tryCancelOrderAsync(url, requestInit, requestBody);
    }

    return response;
  }

  private async tryCancelOrderAsync(
    originalUrl: string,
    originalInit: RequestInit,
    requestBody: string | null,
  ): Promise<void> {
    try {
      const requestId = extractRequestId(requestBody);
      if (requestId == null) return;

      const cancelUrl = buildCancelUrl(originalUrl);
      if (cancelUrl == null) return;

      const headers = new Headers();
      headers.set("Content-Type", "application/json");

      // Copy auth and partner-id headers so the cancel request is properly authenticated
      // and attributed — the inner middleware handles retry and other cross-cutting concerns.
      const originalHeaders = new Headers(originalInit.headers);
      const auth = originalHeaders.get("Authorization");
      if (auth) headers.set("Authorization", auth);
      const partnerId = originalHeaders.get(MissionControlHeaders.PartnerId);
      if (partnerId) headers.set(MissionControlHeaders.PartnerId, partnerId);

      const cancelInit: RequestInit = {
        method: "POST",
        headers,
        body: JSON.stringify({ originalRequestId: requestId }),
        signal: AbortSignal.timeout(CANCEL_TIMEOUT_MS),
      };

      await this.next!.execute(cancelUrl, cancelInit, undefined);
    } catch {
      // Best-effort: swallow failures so the original error propagates.
    }
  }
}

function isAutoCancelEndpoint(url: string): boolean {
  try {
    const path = new URL(url).pathname;
    return AUTO_CANCEL_PATH_PREFIXES.some((prefix) =>
      path.toLowerCase().includes(prefix.toLowerCase()),
    );
  } catch {
    return false;
  }
}

function isServerError(response: Response): boolean {
  return response.status >= 500;
}

function extractRequestId(json: string | null): string | null {
  if (!json) return null;
  try {
    const parsed: unknown = JSON.parse(json);
    if (
      parsed != null &&
      typeof parsed === "object" &&
      "requestId" in parsed &&
      typeof (parsed as Record<string, unknown>).requestId === "string"
    ) {
      return (parsed as Record<string, unknown>).requestId as string;
    }
  } catch {
    // Malformed body — nothing to extract.
  }
  return null;
}

function buildCancelUrl(url: string): string | null {
  try {
    const parsed = new URL(url);
    const path = parsed.pathname;
    const orderIndex = path.toLowerCase().indexOf("/order/");
    if (orderIndex < 0) return null;

    parsed.pathname =
      path.slice(0, orderIndex + "/Order/".length) + "cancelOrder";
    parsed.search = "";
    return parsed.toString();
  } catch {
    return null;
  }
}
