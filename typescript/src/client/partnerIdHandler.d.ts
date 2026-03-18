import type { Middleware, RequestOption } from "@microsoft/kiota-http-fetchlibrary";
/**
 * A Kiota middleware that injects a default {@link MissionControlHeaders.PartnerId} header into
 * every outgoing request. The header is only added when no value has already been set on the
 * request, so a per-request override always takes precedence.
 */
export declare class PartnerIdHandler implements Middleware {
    private readonly defaultPartnerId;
    next: Middleware | undefined;
    /**
     * @param defaultPartnerId The partner ID to add to requests that do not already carry the header.
     */
    constructor(defaultPartnerId: string);
    execute(url: string, requestInit: RequestInit, requestOptions?: Record<string, RequestOption>): Promise<Response>;
}
//# sourceMappingURL=partnerIdHandler.d.ts.map