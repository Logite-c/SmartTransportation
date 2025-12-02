// src/SmartTransportationMenu.tsx
import React, { FC, useCallback, useMemo, useState } from "react";
import {bindLocalValue, useValue} from "cs2/api"
import { Button, Icon, Tooltip } from "cs2/ui";
import { useLocalization } from "cs2/l10n";
import icon from "images/tram-svgrepo-com.svg";
import styles from "mods/SmartTransportationMenu.module.scss";
import CustomRulesPanel from "mods/CustomRulesComponent/CustomRulesPanel";
import AddCustomRulePanel from "mods/AddCustomRuleComponent/AddCustomRulePanel";
import ViewRoutesPanel from "mods/ViewRoutesComponent/ViewRoutesPanel";

interface SectionItem {
	isOpen: boolean;
	toggle: (state: boolean) => void;
	tooltipKey: string;
	defaultTooltip: string;
	displayKey: string;
	defaultDisplay: string;
}

type SectionsType = Record<string, SectionItem>;
const menuVisibleBinding = bindLocalValue(false);
const customRulesVisibleBinding = bindLocalValue(false);
const addRuleVisibleBinding = bindLocalValue(false);
const routesVisibleBinding = bindLocalValue(false);

const menuOpenTrigger = (state: boolean) => {
	menuVisibleBinding.update(state);
}
const customRulesOpenTrigger = (state: boolean) => {
	customRulesVisibleBinding.update(state);
}
const addRuleOpenTrigger = (state: boolean) => {
	addRuleVisibleBinding.update(state);
}
const routesOpenTrigger = (state: boolean) => {
	routesVisibleBinding.update(state);
}



const SmartTransportationMenu: FC = () => {

    
	const { translate } = useLocalization();
    const menuVisible = useValue(menuVisibleBinding);
    const customRulesVisible = useValue(customRulesVisibleBinding);
    const addRuleVisible = useValue(addRuleVisibleBinding);
    const routesVisible = useValue(routesVisibleBinding);

	
	const [rulesRefreshToken, setRulesRefreshToken] = useState(0);

	const sections = useMemo<SectionsType>(
		() => ({
			ViewCustomRules: {
				isOpen: customRulesVisible,
				toggle: customRulesOpenTrigger,
				tooltipKey: "SmartTransportation.Menu[ViewCustomRulesTooltip]",
				defaultTooltip: "View custom rules",
				displayKey: "SmartTransportation.Menu[ViewCustomRules]",
				defaultDisplay: "View Custom Rules",
			},
			AddCustomRule: {
				isOpen: addRuleVisible,
				toggle: addRuleOpenTrigger,
				tooltipKey: "SmartTransportation.Menu[AddCustomRuleTooltip]",
				defaultTooltip: "Create a new custom rule",
				displayKey: "SmartTransportation.Menu[AddCustomRule]",
				defaultDisplay: "Add Custom Rule",
			},
			ViewRoutes: {
				isOpen: routesVisible,
				toggle: routesOpenTrigger,
				tooltipKey: "SmartTransportation.Menu[ViewRoutesTooltip]",
				defaultTooltip: "View routes and assign Rules",
				displayKey: "SmartTransportation.Menu[ViewRoutes]",
				defaultDisplay: "View Routes / Assign Rules",
			},
		}),
		[customRulesVisible, addRuleVisible, routesVisible]
	);

	const toggleSection = useCallback(
		(name: string) => {
			const section = sections[name];
			if (!section) {
				return;
			}

			section.toggle(!section.isOpen);
		},
		[sections]
	);

	

	return (
		<div>
			<Tooltip
				tooltip={translate(
					"SmartTransportation.Menu[FloatingButtonTooltip]",
					"Smart Transportation"
				)}
			>
				<Button
					variant="floating"
					selected={menuVisible}
					onSelect={() => menuOpenTrigger(!menuVisible)}
				>
                    <Icon src={icon} tinted={true} className={styles.icon}/>
                </Button>
			</Tooltip>

			{menuVisible && (
				<div draggable className={styles.panel}>
					<header className={styles.header}>
						<div>
							{translate(
								"SmartTransportation.Menu[Title]",
								"Smart Transportation"
							)}
						</div>
					</header>
					<div className={styles.buttonRow}>
						{Object.entries(sections).map(([name, section]) => (
							<Tooltip
								key={name}
								tooltip={translate(
									section.tooltipKey,
									section.defaultTooltip
								)}
							>
								<Button
									variant="flat"
									aria-label={
										translate(
											section.displayKey,
											section.defaultDisplay
										) ?? section.defaultDisplay
									}
									aria-expanded={section.isOpen}
									className={
										section.isOpen
											? styles.buttonSelected
											: styles.TripsDataViewButton
									}
									onSelect={() => toggleSection(name)}
									onMouseDown={(e) => e.preventDefault()}
								>
									{translate(
										section.displayKey,
										section.defaultDisplay
									)}
								</Button>
							</Tooltip>
						))}
					</div>
				</div>
			)}

			{customRulesVisible && (
				<CustomRulesPanel
					key={rulesRefreshToken}
					onClose={() => customRulesOpenTrigger(false)}
				/>
			)}

			{addRuleVisible && (
				<AddCustomRulePanel
					onClose={() => addRuleOpenTrigger(false)}
					onRuleSaved={() => {
						setRulesRefreshToken((t: number) => t + 1);
					}}
				/>
			)}

			{routesVisible && (
				<ViewRoutesPanel onClose={() => routesOpenTrigger(false)} />
			)}
		</div>
	);
};

export default SmartTransportationMenu;