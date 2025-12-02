import React, { useMemo } from "react";
import { bindLocalValue, bindValue, useValue } from "cs2/api";
import {RouteInfo} from "mods/Domain/routeInfo";
import {DisabledTransportTypes} from "mods/Domain/disabledTranportTypes";
import { Button, DraggablePanelProps, Panel, Scrollable } from "cs2/ui";
import styles from "mods/ViewRoutesComponent/ViewRoutesPanel.module.scss";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";
import { RuleEditor } from "./RuleEditorComponent/ruleEditor";
import { routeInfosBinding$, disabledTypesBinding$, ruleEditorVisibleBinding, ruleEditorOpenTrigger } from "mods/bindings";

interface ViewRoutesPanelProps {
    onClose: () => void;
}

const ViewRoutesPanel: React.FC<ViewRoutesPanelProps & DraggablePanelProps> = ({ onClose }) => {
    const routeInfos = useValue(routeInfosBinding$);
    const disabledTypes = useValue(disabledTypesBinding$);
    const ruleEditorVisible = useValue(ruleEditorVisibleBinding);
    const [selectedRoute, setSelectedRoute] = React.useState<RouteInfo | null>(null);

    const routes: RouteInfo[] = useMemo(() => {
        if (!Array.isArray(routeInfos)) {
            return [];
        }
        // Filter out disabled transport types, then sort
        return routeInfos
            .filter((r) => !disabledTypes[r.transportType as keyof DisabledTransportTypes])
            .sort((a, b) => {
                if (a.transportType === b.transportType) {
                    return a.routeNumber - b.routeNumber;
                }
                return a.transportType.localeCompare(b.transportType);
            });
    }, [routeInfos, disabledTypes]);

    const hasRoutes = routes.length > 0;
    const handleRuleEditorClose = () => {
    setSelectedRoute(null);
    ruleEditorOpenTrigger(false, null);
    };
    return (
        <>
        <Panel
            draggable={true}
            onClose={onClose}
            initialPosition={{x: 0.001,y: 0.5}}
            className={styles.panel}
            header={<div className={styles.header}><span className={styles.headerText}>Smart Transportation - View Routes and Assign Rules</span></div>}
        >
            {!hasRoutes && <div className={styles.noRoutes}>No transit routes found.</div>}

            {hasRoutes && (
                <div className={styles.routesList}>
                    <Scrollable>
                        <div className={styles.titles}>
                            <div>Route Name</div>
                            <div>Transport Type</div>
                            <div>Route Number</div>
                            <div>Assigned Rule</div>
                        </div>
                        {routes.map((r) => (
                            <div
                                key={`${r.transportType}-${r.routeNumber}`}
                                className={`${styles.routeItem} ${selectedRoute?.routeNumber === r.routeNumber && selectedRoute?.transportType === r.transportType ? styles.selectedRouteItem : ''}`}
                                onClick={() => {
                                    const isSameRoute =
                                        selectedRoute?.routeNumber === r.routeNumber &&
                                        selectedRoute?.transportType === r.transportType;

                                    if (isSameRoute) {
                                        handleRuleEditorClose();
                                    } else {
                                        setSelectedRoute(r);
                                        ruleEditorOpenTrigger(true, r);
                                    }
                                }}
                            >
                                <div>
                                    {r.routeName && r.routeName.trim().length > 0
                                        ? `${r.routeName}`
                                        : `${r.transportType} Route ${r.routeNumber}`}
                                </div>
                                <div>
                                    {r.transportType}
                                </div>
                                <div >
                                    {r.routeNumber}
                                </div>
                                <div >
                                    {r.ruleName || "Disabled"}
                                </div>
                            </div>
                        ))}
                    </Scrollable>
                </div>
            )}
        </Panel>
        {ruleEditorVisible && (
	<RuleEditor onClose={handleRuleEditorClose} />
        )}
        </>
    );
};

export default ViewRoutesPanel;