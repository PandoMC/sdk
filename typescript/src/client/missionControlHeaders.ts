/**
 * Header names used by the MissionControl Partner API.
 */
export const MissionControlHeaders = {
  /**
   * Identifies the partner on whose behalf the request is made.
   * This value is provided via the portal on the client credentials page.
   */
  PartnerId: "X-Partner-Id",
} as const;
