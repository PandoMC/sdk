import { ClientSecretCredential } from "@azure/identity";
import { AzureIdentityAuthenticationProvider } from "@microsoft/kiota-authentication-azure";
import {
  FetchRequestAdapter,
  KiotaClientFactory,
  MiddlewareFactory,
} from "@microsoft/kiota-http-fetchlibrary";
import { createClient, type Client } from "./generated/client";
import {
  getBaseUrl,
  getScope,
  MissionControlEnvironment,
} from "./missionControlEnvironment";
import { PartnerIdHandler } from "./partnerIdHandler";

/**
 * Fluent builder for creating a configured {@link Client} instance.
 */
export class ClientBuilder {
  private _baseUrl: string = getBaseUrl(MissionControlEnvironment.Production);
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
   * Builds and returns the configured {@link Client}.
   *
   * @throws {Error} When credentials have not been configured via {@link withAzureAdClientCredentials}.
   */
  build(): Client {
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
    const authProvider = new AzureIdentityAuthenticationProvider(credential, [
      this._scope,
    ]);

    const middlewares = MiddlewareFactory.getDefaultMiddlewares();
    if (this._defaultPartnerId) {
      middlewares.unshift(new PartnerIdHandler(this._defaultPartnerId));
    }

    const httpClient = KiotaClientFactory.create(undefined, middlewares);
    const adapter = new FetchRequestAdapter(
      authProvider,
      undefined,
      undefined,
      httpClient,
    );
    adapter.baseUrl = this._baseUrl;
    return createClient(adapter);
  }
}
