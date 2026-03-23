import {
  type Middleware,
  MiddlewareFactory,
  RetryHandler,
  RetryHandlerOptions,
} from "@microsoft/kiota-http-fetchlibrary";
import { nonIdempotentPaths } from "./nonIdempotentPaths";
import { AutoCancelOrderHandler } from "./autoCancelOrderHandler";
import { PartnerIdHandler } from "./partnerIdHandler";

function isNoRetryEndpoint(url: string): boolean {
  try {
    const { pathname } = new URL(url);
    // Prefix match handles both fixed paths and parameterised ones.
    return nonIdempotentPaths.some((prefix) =>
      pathname.toLowerCase().startsWith(prefix.toLowerCase()),
    );
  } catch {
    return false;
  }
}

/**
 * Builds the middleware stack for a MissionControl HTTP client.
 *
 * The default Kiota retry handler is replaced with a custom one that:
 * - Never retries non-idempotent endpoints (createOrder, claimReservation, reportOrder),
 *   since a retry could cause duplicate orders or reservations.
 * - Retries on 429 Too Many Requests, 503 Service Unavailable, and 504 Gateway Timeout.
 *
 * The {@link AutoCancelOrderHandler} sits at the outermost position so it sees the final
 * response after all retries are exhausted. Its compensating cancel request flows through
 * the inner handlers and therefore picks up partner-id injection and retry behaviour.
 */
export function createHandlers(defaultPartnerId?: string): Middleware[] {
  const retryOptions = new RetryHandlerOptions({
    shouldRetry: (_delay, _attempt, request, _options, response) => {
      if (isNoRetryEndpoint(request)) return false;
      return (
        response.status === 429 ||
        response.status === 503 ||
        response.status === 504
      );
    },
  });

  const middlewares = MiddlewareFactory.getDefaultMiddlewares();

  // Replace the default RetryHandler with our custom-configured one.
  const retryIndex = middlewares.findIndex((m) => m instanceof RetryHandler);
  if (retryIndex !== -1) {
    middlewares[retryIndex] = new RetryHandler(retryOptions);
  }

  if (defaultPartnerId) {
    middlewares.unshift(new PartnerIdHandler(defaultPartnerId));
  }
  middlewares.unshift(new AutoCancelOrderHandler());

  return middlewares;
}
