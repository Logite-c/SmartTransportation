import React, { useEffect, useMemo, useState } from "react";
import { bindValue, useValue } from "cs2/api";
import {RouteInfo} from "mods/Domain/routeInfo";
import {DisabledTransportTypes} from "mods/Domain/disabledTranportTypes";
import { Button, DraggablePanelProps, Panel, Scrollable, Dropdown, DropdownItem, DropdownToggle, Tooltip } from "cs2/ui";import styles from "mods/ViewRoutesComponent/RuleEditorComponent/ruleEditor.module.scss";
import { customRulesBinding$, routeInfosBinding$, setRouteRule } from "mods/bindings";
import { Theme } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { ruleEditorVisibleBinding } from "mods/bindings";
import { CustomRule } from "mods/Domain/customRule";
import { ModuleResolver } from "mods/moduleResolver";







const DropdownStyle: Theme | any = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

const RuleSelector = () => {
	const ruleInfos = useValue(customRulesBinding$);
	const { route } = useValue(ruleEditorVisibleBinding); // contains transportType & routeNumber

	if (!route) {
		return null; // no route selected
	}

	const ruleDropdownItems = ruleInfos.map((ruleInfo) => {
		const selected = ruleInfo.ruleName === route.ruleName;

		return (
			<DropdownItem
				key={ruleInfo.ruleId}
				theme={DropdownStyle}
				value={ruleInfo.ruleId}
				selected={selected}
				onChange={() => {
                    setRouteRule(route.transportType, route.routeNumber, ruleInfo.ruleId);
                    ruleEditorVisibleBinding.update({
                        visible: true,
                        route: { ...route, ruleName: ruleInfo.ruleName }
                    });
                }}
			>
				{ruleInfo.ruleName}
			</DropdownItem>
		);
	});

	return (
		<Tooltip direction="right" tooltip={<ModuleResolver.instance.FormattedParagraphs children={'Select a rule to assign to this route.'}/> }>
			<div className={styles.districtDropdownRow}>
				<Dropdown theme={DropdownStyle} content={ruleDropdownItems}>
					<DropdownToggle>{route.ruleName ?? "Select rule"}</DropdownToggle>
				</Dropdown>
			</div>
		</Tooltip>
	);
};







interface RuleEditorProps extends Partial<DraggablePanelProps> {
    // Make onClose optional and provide a default empty function
    onClose?: () => void;
}

export const RuleEditor = (props: RuleEditorProps): JSX.Element | null => {
const {visible, route} = useValue(ruleEditorVisibleBinding);


    if(!visible){
        return null;
    }

    return (
        <Panel
            draggable={true}
            onClose={props.onClose}
            initialPosition={{x: 0.31,y: 0.332}}
            className={styles.panel}
            header={<div className={styles.header}><span className={styles.headerText}>Rule Editor</span></div>}
        >
            <div className={styles.ruleEditorRow}>
	<div className={styles.ruleEditorSection}>
		<div className={styles.ruleEditorLabel}>Route:</div>
		<div className={styles.ruleEditorContent}>
			{route?.routeName || `${route?.transportType} Route ${route?.routeNumber}`}
		</div>
	</div>

	<div className={styles.ruleEditorSection}>
		<div className={styles.ruleEditorLabel}>Transport type:</div>
		<div className={styles.ruleEditorContent}>{route?.transportType}</div>
	</div>

	<div className={styles.ruleEditorSection}>
		<div className={styles.ruleEditorLabel}>Route number:</div>
		<div className={styles.ruleEditorContent}>{route?.routeNumber}</div>
	</div>

	<div className={styles.ruleEditorSection}>
		<div className={styles.ruleEditorLabel}>Current rule name:</div>
		<div className={styles.ruleEditorContent}>{route?.ruleName}</div>
	</div>

	<div className={styles.ruleEditorSection}>
		<div className={styles.ruleEditorLabel}>New rule name:</div>
		<div className={styles.ruleEditorContent}>
			<RuleSelector />
		</div>
	</div>
</div>   
        </Panel>
    );
}