import React, { useState } from "react";
import { Button } from "cs2/ui";
import { Theme } from "cs2/bindings";
import { getModule } from "cs2/modding";
import styles from "./text-input.module.scss";
import classNames from "classnames";
import { FOCUS_DISABLED } from "cs2/ui";
import arrowLeftClear from "images/RB_ArrowLeftClear.svg";
export const TextInputTheme: Theme | any = getModule(
    "game-ui/editor/widgets/item/editor-item.module.scss",
    "classes"
);

interface TextInputProps {
    onChange?: (val: string) => void;
    value?: string;
    placeholder?: string;
    id?: string;
}

export const TextInput: React.FC<TextInputProps> = (props) => {
    let [internalValue, setInternalValue] = useState<string>(props.value ?? "");


    const handleChange: React.ChangeEventHandler<HTMLInputElement> = ({ target }) => {
        setInternalValue(target.value);
        props.onChange?.(target.value);
    };

    const clearText = () => {
        setInternalValue("");
        props.onChange?.("");
    };

    return (
        <div className={styles.container}>
            <div className={styles.searchArea}>
                <input
                    id={props.id}
                    value={props.value !== undefined ? props.value : internalValue}
                    disabled={false}
                    type="text"
                    className={classNames(TextInputTheme.input, styles.textBox)}
                    onChange={handleChange}
                />

                {(props.value === undefined ? internalValue : props.value) === "" && (
                    <span className={styles.placeholder}>{props.placeholder}</span>
                )}

                {internalValue?.trim() !== "" ? (
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

export default TextInput;
