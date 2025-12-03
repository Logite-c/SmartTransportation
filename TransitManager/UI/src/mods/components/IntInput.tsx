import React, { useState } from "react";
import { Button } from "cs2/ui";
import { Theme } from "cs2/bindings";
import { getModule } from "cs2/modding";
import styles from "mods/components/IntInput.module.scss";
import classNames from "classnames";
import { FOCUS_DISABLED } from "cs2/ui";
import arrowLeftClear from "images/RB_ArrowLeftClear.svg";
export const TextInputTheme: Theme | any = getModule(
    "game-ui/editor/widgets/item/editor-item.module.scss",
    "classes"
);

interface IntInputProps {
    onChange?: (val: number) => void;
    value?: number;
    placeholder?: number;
    id?: string;
}

export const IntInput: React.FC<IntInputProps> = (props) => {
	const [internalValue, setInternalValue] = useState<string>(
		props.value !== undefined ? String(props.value) : ""
	);

	const handleChange: React.ChangeEventHandler<HTMLInputElement> = ({ target }) => {
		const next = target.value;          // string from the input
		setInternalValue(next);

		if (props.onChange) {
			if (next === "") {
				// choose your empty behaviour; here we send 0
				props.onChange(0);
			} else {
				const parsed = Number(next);
				if (!Number.isNaN(parsed)) {
					props.onChange(parsed);
				}
			}
		}
	};

	const clearText = () => {
		setInternalValue("");
		// also reset to 0 on clear (or remove this call if you prefer)
		props.onChange?.(0);
	};

	const displayValue =
		props.value !== undefined ? String(props.value) : internalValue;

	return (
		<div className={styles.container}>
			<div className={styles.searchArea}>
				<input
					id={props.id}
					value={displayValue}
					disabled={false}
					type="number"
					className={classNames(TextInputTheme.input, styles.textBox)}
					onChange={handleChange}
				/>

				{displayValue === "" && (
					<span className={styles.placeholder}>{props.placeholder}</span>
				)}

				{displayValue.trim() !== "" ? (
					<Button
                                            className={styles.clearIcon}
                                            variant="icon"
                                            onSelect={clearText}
                                            focusKey={FOCUS_DISABLED}
                                        >
                                            <img style={{ maskImage: `url(${arrowLeftClear})` }}  alt={""}/>
                                        </Button>
                                        ) : <></>
                                        }
				
			</div>
		</div>
	);
};

export default IntInput;
