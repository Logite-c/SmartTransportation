// src/AddCustomRulePanel.tsx
import React, { useState } from "react";
import { trigger } from "cs2/api";
import { TextInput } from "../components/TextInput";
import { Button, DraggablePanelProps, Panel } from "cs2/ui";
import styles from "mods/AddCustomRuleComponent/AddCustomRulePanel.module.scss";
import IntInput from "../components/IntInput";
import { AddCustomRule } from "mods/Domain/addCustomRule";
import { handleSave } from "mods/bindings";
interface AddCustomRulePanelProps {
     onRuleSaved: () => void;
     onClose: () => void;
}

const AddCustomRulePanel: React.FC<AddCustomRulePanelProps & DraggablePanelProps> = ({
    onClose,
}) => {
    const [ruleName, setRuleName] = useState("");
    const [occupancy, setOccupancy] = useState<number>(80);
    const [stdTicket, setStdTicket] = useState<number>(100);
    const [maxTicketInc, setMaxTicketInc] = useState<number>(20);
    const [maxTicketDec, setMaxTicketDec] = useState<number>(20);
    const [maxVehAdj, setMaxVehAdj] = useState<number>(30);
    const [minVehAdj, setMinVehAdj] = useState<number>(30);

    

    return (
        <Panel
            draggable={true}
            onClose={onClose}
            initialPosition={{
                x: 0.38,
                y: 0.5
            }}
            className={styles.panel}
            header={
            <div className={styles.header}>
              <span className={styles.headerText}>
                Smart Transportation - Add Custom Rule
              </span>
            </div>
          }
            
        >
            <div
                style={{
                    padding: "10px 16px 12px",
                    display: "flex",
                    flexDirection: "column",
                    height: "100%",
                    boxSizing: "border-box",
                }}
            >
                
                <div>
                    <div className={styles.labelStyle}>
                        Rule name
                    </div>
                    <TextInput
                        id="ruleName"
                        value={ruleName}
                        onChange={setRuleName}
                        placeholder="Enter rule name. . ."
                    />
                </div>

                <div>
                    <div>
                        <div className={styles.labelStyle}>
                            Occupancy target (%)
                        </div>
                        <IntInput
                            id="occupancy"
                            value={occupancy}
                            onChange={setOccupancy}
                            placeholder= {80}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Standard ticket
                        </div>
                        <IntInput
                            id="stdTicket"
                            value={stdTicket}
                            onChange={setStdTicket}
                            placeholder={100}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Maximum ticket increase (%)
                        </div>
                        <IntInput
                            id="maxTicketInc"
                            value={maxTicketInc}
                            onChange={setMaxTicketInc}
                            placeholder={20}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Maximum ticket decrease (%)
                        </div>
                        <IntInput
                            id="maxTicketDec"
                            value={maxTicketDec}
                            onChange={setMaxTicketDec}
                            placeholder={20}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Minimum vehicle adjustment (%)
                        </div>
                        <IntInput
                            id="minVehAdj"
                            value={minVehAdj}
                            onChange={setMinVehAdj}
                            placeholder={30}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Maximum vehicle adjustment (%)
                        </div>
                        <IntInput
                            id="maxVehAdj"
                            value={maxVehAdj}
                            onChange={setMaxVehAdj}
                            placeholder={30}
                        />
                    </div>
                </div>

                <div className={styles.buttonSection}>
                    <Button
                        variant="flat"
                        className={styles.buttonStyle}
                        onSelect={onClose}
                    >
                        Cancel
                    </Button>
                    <Button
                        variant="flat"
                        className={styles.buttonStyle}
                        onSelect={() => {
                            handleSave({
                                ruleName,
                                occupancy,
                                stdTicket,
                                maxTicketInc,
                                maxTicketDec,
                                maxVehAdj,
                                minVehAdj
                            });
                            onClose();
                        }}
                        >
                        Save
                    </Button>
                </div>
                
            </div>
        </Panel>
    );
};

export default AddCustomRulePanel;
