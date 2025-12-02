// index.tsx
import { ModRegistrar } from "cs2/modding";
import SmartTransportationMenu from "mods/SmartTransportationMenu";
import "intl";
import "intl/locale-data/jsonp/en-US";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";

const register: ModRegistrar = (moduleRegistry) => {
    // Same place as TripsView: top left overlay
    VanillaComponentResolver.setRegistry(moduleRegistry);

    moduleRegistry.append("GameTopLeft", SmartTransportationMenu);
};

export default register;
