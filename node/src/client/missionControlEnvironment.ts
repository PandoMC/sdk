/**
 * Represents a MissionControl API environment, encapsulating its base URL and OAuth scope.
 */
export enum MissionControlEnvironment {
  /** https://api.sandbox.missioncontrol.io */
  Sandbox = "Sandbox",

  /** https://api.missioncontrol.io */
  Production = "Production",
}

const settings: Record<
  MissionControlEnvironment,
  { baseUrl: string; scope: string }
> = {
  [MissionControlEnvironment.Sandbox]: {
    baseUrl: "https://api.sandbox.missioncontrol.io/partnerApi/v2",
    scope: "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default",
  },
  [MissionControlEnvironment.Production]: {
    baseUrl: "https://api.missioncontrol.io/partnerApi/v2",
    scope: "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default",
  },
};

export function getBaseUrl(env: MissionControlEnvironment): string {
  return settings[env].baseUrl;
}

export function getScope(env: MissionControlEnvironment): string {
  return settings[env].scope;
}
