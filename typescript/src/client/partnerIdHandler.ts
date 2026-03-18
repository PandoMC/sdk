import type {
  Middleware,
  RequestOption,
} from "@microsoft/kiota-http-fetchlibrary";
import { MissionControlHeaders } from "./missionControlHeaders";

/**
 * A Kiota middleware that injects a default {@link MissionControlHeaders.PartnerId} header into
 * every outgoing request. The header is only added when no value has already been set on the
 * request, so a per-request override always takes precedence.
 */
export class PartnerIdHandler implements Middleware {
  private readonly defaultPartnerId: string;
  next: Middleware | undefined;

  /**
   * @param defaultPartnerId The partner ID to add to requests that do not already carry the header.
   */
  constructor(defaultPartnerId: string) {
    this.defaultPartnerId = defaultPartnerId;
  }

  async execute(
    url: string,
    requestInit: RequestInit,
    requestOptions?: Record<string, RequestOption>,
  ): Promise<Response> {
    const headers = new Headers(requestInit.headers);
    if (!headers.has(MissionControlHeaders.PartnerId)) {
      headers.set(MissionControlHeaders.PartnerId, this.defaultPartnerId);
      requestInit = { ...requestInit, headers };
    }
    return this.next!.execute(url, requestInit, requestOptions);
  }
}
