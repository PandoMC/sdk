import { ClientSecretCredential } from "@azure/identity";
import { AzureIdentityAuthenticationProvider } from "@microsoft/kiota-authentication-azure";
import {
  FetchRequestAdapter,
  KiotaClientFactory,
} from "@microsoft/kiota-http-fetchlibrary";
import {
  getAssetsBaseUrl,
  getBaseUrl,
  getScope,
  MissionControlEnvironment,
} from "./missionControlEnvironment";
import { createClient, type Client } from "./generated/client";
import { createAssetsClient, type AssetsClient } from "./generatedAssets/assetsClient";
import { createHandlers } from "./missionControlHandlers";

/**
 * The Mission:Control Partner API client.
 */
export type MissionControlClient = Client & {
  /**
   * The Assets API: asset packs (images, logos, etc.) publishers provide for their products.
   * It is served by a separate service, so it has its own base URL but shares authentication and headers.
   */
  readonly assets: AssetsClient;
};

/**
 * Fluent builder for creating a configured {@link MissionControlClient} instance.
 */
export class ClientBuilder {
  private _baseUrl: string = getBaseUrl(MissionControlEnvironment.Production);
  private _assetsBaseUrl: string = getAssetsBaseUrl(MissionControlEnvironment.Production);
  private _scope: string = getScope(MissionControlEnvironment.Production);
  private _tenantId: string = "13d084c1-d072-41f2-878b-c45ca721c9f6";
  private _clientId?: string;
  private _clientSecret?: string;
  private _defaultPartnerId?: string;

  /**
   * Sets the default `X-Partner-Id` header value sent with every request.
   *
   * Most partners operate under a single partner identity, so setting this once on the
   * builder means you never have to specify it per-request. When a request explicitly sets
   * the header that value takes precedence over the default configured here.
   *
   * @param partnerId The partner ID shown on the client credentials page in the portal.
   */
  withDefaultPartnerId(partnerId: string): this {
    if (!partnerId?.trim())
      throw new Error("partnerId must not be null or whitespace.");
    this._defaultPartnerId = partnerId;
    return this;
  }

  /**
   * Overrides the base URL used for all API requests.
   * Useful for pointing at a local development server or a custom endpoint.
   * When combined with {@link forEnvironment}, the last call wins.
   *
   * @param baseUrl The base URL, e.g. `http://localhost:5000`.
   */
  withBaseUrl(baseUrl: string): this {
    if (!baseUrl?.trim())
      throw new Error("baseUrl must not be null or whitespace.");
    this._baseUrl = baseUrl;
    return this;
  }

  /**
   * Overrides the base URL used for Assets API requests (`client.assets`).
   * The Assets API is a separate service, so it is not affected by {@link withBaseUrl}.
   * When combined with {@link forEnvironment}, the last call wins.
   *
   * @param assetsBaseUrl The base URL, e.g. `http://localhost:5001/partnerApi/v1`.
   */
  withAssetsBaseUrl(assetsBaseUrl: string): this {
    if (!assetsBaseUrl?.trim())
      throw new Error("assetsBaseUrl must not be null or whitespace.");
    this._assetsBaseUrl = assetsBaseUrl;
    return this;
  }

  /**
   * Overrides the OAuth scope used when acquiring tokens.
   * Useful when running against a local or custom environment whose app registration
   * differs from the well-known environments.
   * When combined with {@link forEnvironment}, the last call wins.
   *
   * @param scope The full scope URI, e.g. `api://<app-id>/.default`.
   */
  withScope(scope: string): this {
    if (!scope?.trim())
      throw new Error("scope must not be null or whitespace.");
    this._scope = scope;
    return this;
  }

  /**
   * Targets a well-known MissionControl environment. Sets the matching base URL and API scope together.
   * Defaults to {@link MissionControlEnvironment.Production} when not called.
   *
   * @param environment The target environment.
   */
  forEnvironment(environment: MissionControlEnvironment): this {
    this._baseUrl = getBaseUrl(environment);
    this._assetsBaseUrl = getAssetsBaseUrl(environment);
    this._scope = getScope(environment);
    return this;
  }

  /**
   * Configures the client to authenticate using Azure AD client credentials (client secret flow).
   * The API scope is derived from the configured environment unless overridden by {@link withScope}.
   *
   * @param clientId The application (client) ID of the app registration.
   * @param clientSecret The client secret of the app registration.
   * @param tenantId The Azure AD tenant ID. Uses the PandoMC tenant when omitted.
   */
  withAzureAdClientCredentials(
    clientId: string,
    clientSecret: string,
    tenantId?: string,
  ): this {
    this._clientId = clientId;
    this._clientSecret = clientSecret;
    if (tenantId) this._tenantId = tenantId;
    return this;
  }

  /**
   * Builds and returns the configured {@link MissionControlClient}.
   *
   * @throws {Error} When credentials have not been configured via {@link withAzureAdClientCredentials}.
   */
  build(): MissionControlClient {
    if (!this._clientId || !this._clientSecret) {
      throw new Error(
        "No credentials configured. " +
          "Call withAzureAdClientCredentials() before calling build().",
      );
    }

    const credential = new ClientSecretCredential(
      this._tenantId,
      this._clientId,
      this._clientSecret,
    );
    const allowedHosts = new Set([
      new URL(this._baseUrl).host,
      new URL(this._assetsBaseUrl).host,
    ]);
    const authProvider = new AzureIdentityAuthenticationProvider(
      credential,
      [this._scope],
      undefined,
      allowedHosts,
    );

    const middlewares = createHandlers(this._defaultPartnerId);
    const httpClient = KiotaClientFactory.create(undefined, middlewares);
    const adapter = new FetchRequestAdapter(
      authProvider,
      undefined,
      undefined,
      httpClient,
    );
    adapter.baseUrl = this._baseUrl;

    // The Assets API is a separate service: same HTTP pipeline and credentials, different base URL.
    const assetsAdapter = new FetchRequestAdapter(
      authProvider,
      undefined,
      undefined,
      httpClient,
    );
    assetsAdapter.baseUrl = this._assetsBaseUrl;
    const assets = createAssetsClient(assetsAdapter);

    // The generated client is a proxy, so expose assets through a wrapping proxy rather than mutating it.
    return new Proxy(createClient(adapter), {
      get: (target, property, receiver) =>
        property === "assets" ? assets : Reflect.get(target, property, receiver),
    }) as MissionControlClient;
  }
}
