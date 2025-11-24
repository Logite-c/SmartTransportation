// src/CustomRulesPanel.tsx
import React, { useMemo } from "react";
import { bindValue, useValue } from "cs2/api";
import Panel from "mods/panel";

type CustomRule = {
    ruleId: string;
    ruleName: string;
    occupancy: number;
    stdTicket: number;
    maxTicketInc: number;
    maxTicketDec: number;
    maxVehAdj: number;
    minVehAdj: number;
};

interface CustomRulesPanelProps {
    onClose: () => void;
}

const CustomRulesPanel: React.FC<CustomRulesPanelProps> = ({ onClose }) => {
    // Bind to smartTransportation.customRulesJson (stringified JSON from C#)
    const rulesJsonBinding = useMemo(
        () => bindValue<string>("smartTransportation", "customRulesJson"),
        []
    );

    const rawRulesJson = useValue(rulesJsonBinding);

    const rules: CustomRule[] = useMemo(() => {
        const json =
            typeof rawRulesJson === "string" && rawRulesJson.length > 0
                ? rawRulesJson
                : "[]";

        try {
            const parsed = JSON.parse(json);
            if (!Array.isArray(parsed)) {
                return [];
            }
            return parsed as CustomRule[];
        } catch {
            return [];
        }
    }, [rawRulesJson]);

    const hasRules = rules.length > 0;

    return (
        <Panel
            title="SmartTransportation - Custom Rules"
            onClose={onClose}
            initialPosition={{
                top: window.innerHeight * 0.05,
                left: window.innerWidth * 0.30,
            }}
            initialSize={{ width: 700, height: 450 }}
            style={{
                display: "flex",
                flexDirection: "column",
                backgroundColor: "var(--panelColorNormal)",
                color: "#ffffff",
            }}
        >
            <div
                style={{
                    padding: "10px 16px 16px",
                    overflow: "auto",
                    height: "100%",
                }}
            >
                {!hasRules && (
                    <div style={{ opacity: 0.8, fontSize: 14 }}>
                        No Custom Rules
                    </div>
                )}

                {hasRules && (
                    <ul
                        style={{
                            listStyle: "none",
                            margin: 0,
                            padding: 0,
                            display: "flex",
                            flexDirection: "column",
                            gap: 8,
                        }}
                    >
                        {rules.map((r) => (
                            <li
                                key={r.ruleId}
                                style={{
                                    borderBottom:
                                        "1px solid rgba(255,255,255,0.12)",
                                    paddingBottom: 8,
                                }}
                            >
                                <div
                                    style={{
                                        fontWeight: 600,
                                        marginBottom: 4,
                                        fontSize: 15,
                                    }}
                                >
                                    {r.ruleName && r.ruleName.trim().length > 0
                                        ? r.ruleName
                                        : "Unnamed rule"}
                                </div>
                                <div
                                    style={{
                                        fontSize: 13,
                                        opacity: 0.9,
                                        whiteSpace: "normal",
                                    }}
                                >
                                    {`Occupancy target: ${r.occupancy}% | Standard ticket: ${r.stdTicket}` +
                                        ` | Maximum ticket increase: ${r.maxTicketInc}% | Maximum ticket decrease: ${r.maxTicketDec}%` +
                                        ` | Minimum vehicle adjustment: ${r.minVehAdj}% | Maximum vehicle adjustment: ${r.maxVehAdj}%`}
                                </div>
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </Panel>
    );
};

export default CustomRulesPanel;
