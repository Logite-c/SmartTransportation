// src/CustomRulesPanel.tsx
import React, { useMemo } from "react";
import { useValue } from "cs2/api";
import { Button, Icon, Panel, Portal, Scrollable } from "cs2/ui";
import { useLocalization, Localized, Unit, LocElementType } from "cs2/l10n";
import { game } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { CustomRule } from "mods/Domain/customRule";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";
import styles from "mods/CustomRulesComponent/CustomRules.module.scss";
import classNames from "classnames";
import { customRulesBinding$, deleteCustomRule } from "mods/bindings";
import deleteSrc from "images/delete.svg";


const uilStandard = "coui://uil/Standard/";
const closeSrc = uilStandard + "XClose.svg";
const roundButtonHighlightStyle = getModule(
	"game-ui/common/input/button/themes/round-highlight-button.module.scss",
	"classes"
);

const builtInRuleNames = new Set([
	"Bus",
	"Tram",
	"Train",
	"Subway",
	"Ship",
	"Airplane",
	"Ferry",
]);


interface CustomRulesPanelProps {
	onClose: () => void;
}

const CustomRulesPanel: React.FC<CustomRulesPanelProps> = ({ onClose }) => {
	const rules: CustomRule[] = useValue(customRulesBinding$);
	const { translate } = useLocalization();

	// Hide "Disabled" in this panel only
	const displayRules = useMemo(
		() => rules.filter((r) => r.ruleName !== "Disabled"),
		[rules]
	);
	
	const hasRules = displayRules.length > 0;

	

	return (
		<Portal>
			<Panel
				draggable={true}
                onClose={onClose}
				initialPosition={{
					x: 0.001,
					y: 0.5,
				}}
				className={styles.panel}
                header={
                    <div className={styles.header}>
                        <span className={styles.headerText}>
                            Smart Transportation - Custom Rules
                        </span>
                    </div>
                }
			>
				<div className={styles.rowGroup}>
					{!hasRules && (
						<div className={styles.noRules}>
							{translate(
								"SmartTransportation.CustomRules[NoRules]",
								"No custom rules."
							)}
						</div>
					)}
					{hasRules && (
						<Scrollable>
							<div className={styles.rowGroup}>
								<div
									className={classNames(
										styles.columnGroup,
										styles.leftColumn
									)}
								>

									<div className={styles.subtitleRowRuleName}>
										{translate(
											"SmartTransportation.CustomRules[Delete]",
											"Delete"
										)}
									</div>
									{displayRules.map((ruleConfig: CustomRule) => {
										const isBuiltIn = builtInRuleNames.has(ruleConfig.ruleName);

										return (
											<div
												key={ruleConfig.ruleId}
												className={styles.definedHeightRuleName}
											>
												{!isBuiltIn && (
													<Button
														className={roundButtonHighlightStyle.button}
														variant="icon"
														onClick={() => deleteCustomRule(ruleConfig.ruleId)}
													>
														<Icon src={deleteSrc} tinted />
													</Button>
												)}
												{/* For built-in rules we just render an empty cell */}
											</div>
										);
									})}

									{/* Spacer if uneven split */}
								</div>
								<div
									className={classNames(
										styles.columnGroup,
										styles.leftColumn
									)}
								>

									<div className={styles.subtitleRowRuleName}>
										{translate(
											"SmartTransportation.CustomRules[RuleName]",
											"Rule Name"
										)}
									</div>
									{displayRules.map((ruleConfig: CustomRule) => (
										<div
											key={ruleConfig.ruleName}
											className={styles.definedHeightRuleName}
										>
											{ruleConfig.ruleName}
										</div>
										))}
									{/* Spacer if uneven split */}
								</div>
								<div
									className={classNames(
										styles.columnGroup,
										styles.leftColumn
									)}
								>
									<div className={styles.subtitleRow}>
										{translate(
											"SmartTransportation.CustomRules[Occupancy]",
											"Occupancy target"
										)}
									</div>
									{displayRules.map((ruleConfig) => (
										<div
											key={ruleConfig.ruleName}
											className={styles.definedHeight}
										>
											<Localized
												value={{
													__Type: "Game.UI.Localization.LocalizedNumber",
													value: ruleConfig.occupancy,
													unit: Unit.Percentage,
													signed: false,
												} as any}
											/>
										</div>
									))}
									{/* Spacer if uneven split */}
								</div>
								<div
									className={classNames(
										styles.columnGroup,
										styles.leftColumn
									)}
								>
									<div className={styles.subtitleRow}>
										{translate(
											"SmartTransportation.CustomRules[StdTicket]",
											"Standard ticket"
										)}
									</div>
									{displayRules.map((ruleConfig) => (
										<div
											key={ruleConfig.ruleName}
											className={styles.definedHeight}
										>
											<Localized
												value={{
													__Type: "Game.UI.Localization.LocalizedNumber",
													value: ruleConfig.stdTicket,
													unit: Unit.Money,
													signed: false,
												} as any}
											/>
										</div>
										))}
									{/* Spacer if uneven split */}
								</div>
								<div
									className={classNames(
										styles.columnGroup,
										styles.rightColumn
									)}
								>
									<div className={styles.subtitleRow}>
										{translate(
											"SmartTransportation.CustomRules[MaxTicketIncDec]",
											"Max ticket increase / decrease"
										)}
									</div>
									{displayRules.map((ruleConfig) => (
                                        <div
                                            key={ruleConfig.ruleName}
                                            className={styles.definedHeight}
                                        >
                                            <Localized
                                                value={{
                                                    __Type: "Game.UI.Localization.LocalizedNumber",
                                                    value: ruleConfig.maxTicketInc,
                                                    unit: Unit.Percentage,
                                                    signed: false,
                                                } as any}
                                            />
                                            <div className={styles.forwardSlash}> / </div>
                                            <Localized
                                                value={{
                                                    __Type: "Game.UI.Localization.LocalizedNumber",
                                                    value: ruleConfig.maxTicketDec,
                                                    unit: Unit.Percentage,
                                                    signed: false,
                                                } as any}
                                            />
                                        </div>
                                    ))}
									{/* Spacer if uneven split */}
								</div>
								<div
									className={classNames(
										styles.columnGroup,
										styles.rightColumn
									)}
								>
									<div className={styles.subtitleRow}>
										{translate(
											"SmartTransportation.CustomRules[VehAdjRange]",
											"Max / Min vehicle adjustment"
										)}
									</div>
									{displayRules.map((ruleConfig) => (
										<div
											key={ruleConfig.ruleName}
											className={styles.definedHeight}
										>
											<Localized
												value={{
													__Type: "Game.UI.Localization.LocalizedNumber",
													value: ruleConfig.maxVehAdj,
													unit: Unit.Percentage,
													signed: false,
												} as any}
											/>
											<div className={styles.forwardSlash}> / </div>
											<Localized
												value={{
													__Type: "Game.UI.Localization.LocalizedNumber",
													value: ruleConfig.minVehAdj,
													unit: Unit.Percentage,
													signed: false,
												} as any}
											/>
										</div>
									))}
									{/* Spacer if uneven split */}
								</div>
							</div>
						</Scrollable>
					)}
				</div>
			</Panel>
		</Portal>
	);
};

export default CustomRulesPanel;