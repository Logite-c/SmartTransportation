// src/SmartTransportationMenu.tsx
import React, { FC, useCallback, useState } from "react";
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/tram-svgrepo-com.svg";
import styles from "./SmartTransportationMenu.module.scss";
import CustomRulesPanel from "./CustomRulesPanel";
import AddCustomRulePanel from "./AddCustomRulePanel";
import ViewRoutesPanel from "./ViewRoutesPanel";

const SmartTransportationMenu: FC = () => {
    const [menuOpen, setMenuOpen] = useState(false);
    const [customRulesOpen, setCustomRulesOpen] = useState(false);
    const [addRuleOpen, setAddRuleOpen] = useState(false);
    const [routesOpen, setRoutesOpen] = useState(false);
    const [rulesRefreshToken, setRulesRefreshToken] = useState(0);

    const toggleMenu = useCallback(() => {
        setMenuOpen((prev) => !prev);
    }, []);

    return (
        <div>
            <Tooltip tooltip="Smart Transportation">
                <FloatingButton
                    onClick={toggleMenu}
                    src={icon}
                    aria-label="Toggle Smart Transportation Menu"
                />
            </Tooltip>

            {menuOpen && (
                <div draggable={true} className={styles.panel}>
                    <header className={styles.header}>
                        <h2>Smart Transportation</h2>
                    </header>
                    <div className={styles.buttonRow}>
                        <Tooltip tooltip="View custom rules">
                            <Button
                                variant="flat"
                                aria-label="View Custom Rules"
                                aria-expanded={customRulesOpen}
                                className={
                                    customRulesOpen
                                        ? styles.buttonSelected
                                        : styles.TripsDataViewButton
                                }
                                onClick={() => setCustomRulesOpen((v) => !v)}
                                onMouseDown={(e) => e.preventDefault()}
                            >
                                View Custom Rules
                            </Button>
                        </Tooltip>

                        <Tooltip tooltip="Create a new custom rule">
                            <Button
                                variant="flat"
                                aria-label="Add Custom Rule"
                                aria-expanded={addRuleOpen}
                                className={
                                    addRuleOpen
                                        ? styles.buttonSelected
                                        : styles.TripsDataViewButton
                                }
                                onClick={() => setAddRuleOpen(true)}
                                onMouseDown={(e) => e.preventDefault()}
                            >
                                Add Custom Rule
                            </Button>
                        </Tooltip>

                        <Tooltip tooltip="View Routes">
                            <Button
                                variant="flat"
                                aria-label="View Routes"
                                aria-expanded={routesOpen}
                                className={
                                    routesOpen
                                        ? styles.buttonSelected
                                        : styles.TripsDataViewButton
                                }
                                onClick={() => setRoutesOpen(true)}
                                onMouseDown={(e) => e.preventDefault()}
                            >
                                View Routes
                            </Button>
                        </Tooltip>
                    </div>
                </div>
            )}

            {customRulesOpen && (
                <CustomRulesPanel
                    key={rulesRefreshToken}
                    onClose={() => setCustomRulesOpen(false)}
                />
            )}

            {addRuleOpen && (
                <AddCustomRulePanel
                    onClose={() => setAddRuleOpen(false)}
                    onRuleSaved={() => {
                        setRulesRefreshToken((t) => t + 1);
                    }}
                />
            )}

            {routesOpen && (
                <ViewRoutesPanel onClose={() => setRoutesOpen(false)} />
            )}
        </div>
    );
};

export default SmartTransportationMenu;