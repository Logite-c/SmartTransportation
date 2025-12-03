import { bindLocalValue, bindValue, trigger } from "cs2/api";
import { CustomRule } from "./Domain/customRule";
import { AddCustomRule } from "mods/Domain/addCustomRule";
import { RouteInfo } from "./Domain/routeInfo";
import { DisabledTransportTypes } from "./Domain/disabledTranportTypes";
import mod from "mod.json"

export const handleSave = (AddCustomRule:AddCustomRule)   => trigger("smartTransportation", "addCustomRule", AddCustomRule);

export const customRulesBinding$ = bindValue<CustomRule[]>("smartTransportation", "customRulesJson", []);
export const deleteCustomRule = (ruleID: string) => trigger(mod.id, "deleteCustomRule", ruleID);

export const ruleEditorVisibleBinding = bindLocalValue<{visible: boolean, route: RouteInfo | null}>({visible: false, route: null});
export const ruleEditorOpenTrigger = (state: boolean, route: RouteInfo | null) => {
    ruleEditorVisibleBinding.update({visible: state, route: route});
}
export const routeInfosBinding$ = bindValue<RouteInfo[]>("SmartTransportation", "routeInfos", []);
export const setRouteRule = (transportType: string, ruleNumber: number, ruleID: string) => trigger("SmartTransportation", "setRouteRuleForRoute", transportType, ruleNumber, ruleID);
export const disabledTypesBinding$ = bindValue<DisabledTransportTypes>("SmartTransportation", "disabledTransportTypes", {
    Bus: false,
    Tram: false,
    Subway: false,
    Train: false,
    Ship: false,
    Airplane: false,
    Ferry: false,
});
