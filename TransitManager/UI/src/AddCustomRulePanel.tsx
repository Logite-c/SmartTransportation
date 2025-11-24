// src/AddCustomRulePanel.tsx
import React, { useState } from "react";
import { trigger } from "cs2/api";
import Panel from "mods/panel";

interface AddCustomRulePanelProps {
    onClose: () => void;
    onRuleSaved?: () => void;
}

const AddCustomRulePanel: React.FC<AddCustomRulePanelProps> = ({
    onClose,
    onRuleSaved,
}) => {
    const [ruleName, setRuleName] = useState("");
    const [occupancy, setOccupancy] = useState("80");
    const [stdTicket, setStdTicket] = useState("100");
    const [maxTicketInc, setMaxTicketInc] = useState("20");
    const [maxTicketDec, setMaxTicketDec] = useState("20");
    const [maxVehAdj, setMaxVehAdj] = useState("30");
    const [minVehAdj, setMinVehAdj] = useState("30");

    const parseIntSafe = (value: string) => {
        const n = parseInt(value, 10);
        if (isNaN(n)) {
            return 0;
        }
        return n;
    };

    const handleSave = () => {
        const payload = {
            ruleName: ruleName,
            occupancy: parseIntSafe(occupancy),
            stdTicket: parseIntSafe(stdTicket),
            maxTicketInc: parseIntSafe(maxTicketInc),
            maxTicketDec: parseIntSafe(maxTicketDec),
            maxVehAdj: parseIntSafe(maxVehAdj),
            minVehAdj: parseIntSafe(minVehAdj),
        };

        try {
            trigger(
                "smartTransportation",
                "addCustomRule",
                JSON.stringify(payload)
            );
        } catch (err) {
            // Swallow errors here; main logic is in C#
            console.error(
                "[SmartTransportation] Failed to trigger addCustomRule",
                err
            );
        }

        // Tell the menu that we just saved a rule
        if (onRuleSaved) {
            onRuleSaved();
        }

        onClose();
    };

    const labelStyle: React.CSSProperties = {
        display: "block",
        fontSize: "12px",
        marginBottom: 2,
    };

    const inputStyle: React.CSSProperties = {
        width: "100%",
        padding: "4px 6px",
        fontSize: "13px",
        borderRadius: 4,
        border: "1px solid rgba(255,255,255,0.2)",
        backgroundColor: "rgba(0,0,0,0.5)",
        color: "#ffffff",
        boxSizing: "border-box",
    };

    const numericColumnsStyle: React.CSSProperties = {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: 8,
    };

    return (
        <Panel
            title="SmartTransportation - Add Custom Rule"
            onClose={onClose}
            initialPosition={{
                top: window.innerHeight * 0.10,
                left: window.innerWidth * 0.35,
            }}
            initialSize={{ width: 520, height: 380 }}
            style={{
                display: "flex",
                flexDirection: "column",
                backgroundColor: "var(--panelColorNormal)",
                color: "#ffffff",
            }}
        >
            <div
                style={{
                    padding: "10px 16px 12px",
                    display: "flex",
                    flexDirection: "column",
                    gap: 10,
                    height: "100%",
                    boxSizing: "border-box",
                }}
            >
                <div>
                    <label style={labelStyle} htmlFor="ruleName">
                        Rule name
                    </label>
                    <input
                        id="ruleName"
                        type="text"
                        value={ruleName}
                        onChange={(e) => setRuleName(e.target.value)}
                        style={inputStyle}
                    />
                </div>

                <div style={numericColumnsStyle}>
                    <div>
                        <label style={labelStyle} htmlFor="occupancy">
                            Occupancy target (%)
                        </label>
                        <input
                            id="occupancy"
                            type="number"
                            value={occupancy}
                            onChange={(e) => setOccupancy(e.target.value)}
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle} htmlFor="stdTicket">
                            Standard ticket
                        </label>
                        <input
                            id="stdTicket"
                            type="number"
                            value={stdTicket}
                            onChange={(e) => setStdTicket(e.target.value)}
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle} htmlFor="maxTicketInc">
                            Maximum ticket increase (%)
                        </label>
                        <input
                            id="maxTicketInc"
                            type="number"
                            value={maxTicketInc}
                            onChange={(e) => setMaxTicketInc(e.target.value)}
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle} htmlFor="maxTicketDec">
                            Maximum ticket decrease (%)
                        </label>
                        <input
                            id="maxTicketDec"
                            type="number"
                            value={maxTicketDec}
                            onChange={(e) => setMaxTicketDec(e.target.value)}
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle} htmlFor="minVehAdj">
                            Minimum vehicle adjustment (%)
                        </label>
                        <input
                            id="minVehAdj"
                            type="number"
                            value={minVehAdj}
                            onChange={(e) => setMinVehAdj(e.target.value)}
                            style={inputStyle}
                        />
                    </div>

                    <div>
                        <label style={labelStyle} htmlFor="maxVehAdj">
                            Maximum vehicle adjustment (%)
                        </label>
                        <input
                            id="maxVehAdj"
                            type="number"
                            value={maxVehAdj}
                            onChange={(e) => setMaxVehAdj(e.target.value)}
                            style={inputStyle}
                        />
                    </div>
                </div>

                <div
                    style={{
                        display: "flex",
                        justifyContent: "flex-end",
                        marginTop: "auto",
                        gap: 8,
                    }}
                >
                    <button
                        style={{
                            padding: "4px 10px",
                            fontSize: "13px",
                            borderRadius: 4,
                            border: "1px solid rgba(255,255,255,0.3)",
                            backgroundColor: "transparent",
                            color: "#ffffff",
                            cursor: "pointer",
                        }}
                        onClick={onClose}
                    >
                        Cancel
                    </button>
                    <button
                        style={{
                            padding: "4px 10px",
                            fontSize: "13px",
                            borderRadius: 4,
                            border: "1px solid rgba(0,200,255,0.8)",
                            backgroundColor: "rgba(0,120,200,0.9)",
                            color: "#ffffff",
                            cursor: "pointer",
                        }}
                        onClick={handleSave}
                    >
                        Save
                    </button>
                </div>
            </div>
        </Panel>
    );
};

export default AddCustomRulePanel;
