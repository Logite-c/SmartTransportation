// index.tsx
import { ModRegistrar } from "cs2/modding";
import SmartTransportationMenu from "./src/SmartTransportationMenu";
import "intl";
import "intl/locale-data/jsonp/en-US";

const register: ModRegistrar = (moduleRegistry) => {
    // Same place as TripsView: top left overlay
    moduleRegistry.append("GameTopLeft", SmartTransportationMenu);
};

export default register;
