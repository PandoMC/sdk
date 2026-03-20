/**
 * Path prefixes for non-idempotent order-mutation endpoints.
 * Used both to suppress retries and to trigger auto-cancel on failure.
 */
export const nonIdempotentPaths = [
  "/Order/createOrder",
  "/Order/claimReservation",
  "/Order/reportOrder",
] as const;
