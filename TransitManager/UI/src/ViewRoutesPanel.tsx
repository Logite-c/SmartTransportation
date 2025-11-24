import React, { useMemo } from "react";
import { bindValue, useValue } from "cs2/api";
import Panel from "mods/panel";

type RouteInfo = {
    routeNumber: number;
    routeName: string;
    transportType: string;
    ruleName: string;
    ruleId: string;
};

interface ViewRoutesPanelProps {
    onClose: () => void;
}

const ViewRoutesPanel: React.FC<ViewRoutesPanelProps> = ({ onClose }) => {
    const routesJsonBinding = useMemo(
        () => bindValue<string>("smartTransportation", "routesJson"),
        []
    );

    const routesJson = useValue(routesJsonBinding) ?? "[]";

    const routes: RouteInfo[] = useMemo(() => {
        try {
            const parsed = JSON.parse(routesJson) as RouteInfo[];
            if (!Array.isArray(parsed)) {
                return [];
            }
            // Sort: transport type, then route number
            return parsed.slice().sort((a, b) => {
                if (a.transportType === b.transportType) {
                    return a.routeNumber - b.routeNumber;
                }
                return a.transportType.localeCompare(b.transportType);
            });
        } catch {
            return [];
        }
    }, [routesJson]);

    const hasRoutes = routes.length > 0;

    return (
        <Panel
            title="SmartTransportation - Routes"
            onClose={onClose}
            initialSize={{ width: 720, height: 460 }}
            initialPosition={{
                top: window.innerHeight * 0.08,
                left: window.innerWidth * 0.1,
            }}
            style={{
                backgroundColor: "var(--panelColorNormal)",
                color: "#ffffff",
                fontSize: "14px",
                lineHeight: "18px",
            }}
        >
            {!hasRoutes && (
                <div style={{ padding: "8px" }}>No transit routes found.</div>
            )}

            {hasRoutes && (
                <div
                    style={{
                        padding: "8px",
                        display: "flex",
                        flexDirection: "column",
                        gap: 8,
                        maxHeight: "100%",
                        overflowY: "auto",
                    }}
                >
                    {routes.map((r) => (
                        <div
                            key={`${r.transportType}-${r.routeNumber}`}
                            style={{
                                padding: "6px 8px",
                                borderRadius: 4,
                                backgroundColor: "rgba(0,0,0,0.4)",
                                border: "1px solid rgba(255,255,255,0.25)",
                            }}
                        >
                            <div
                                style={{
                                    fontWeight: 700,
                                    marginBottom: 2,
                                    fontSize: "15px",
                                }}
                            >
                                {r.routeName && r.routeName.trim().length > 0
                                    ? r.routeName
                                    : `${r.transportType} Route ${r.routeNumber}`}
                            </div>
                            <div
                                style={{
                                    fontSize: "13px",
                                    opacity: 0.9,
                                    whiteSpace: "normal",
                                }}
                            >
                                {`Transport type: ${r.transportType} | Route number: ${r.routeNumber} | Assigned rule: ${r.ruleName || "Disabled"
                                    }`}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </Panel>
    );
};

export default ViewRoutesPanel;