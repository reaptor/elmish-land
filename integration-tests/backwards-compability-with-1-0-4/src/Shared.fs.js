import { Union } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Types.js";
import { union_type } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Reflection.js";
import { Command_none } from "../.elmish-land/Base/Command.fs.js";
import { empty } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/List.js";

export class SharedMsg extends Union {
    constructor() {
        super();
        this.tag = 0;
        this.fields = [];
    }
    cases() {
        return ["NoOp"];
    }
}

export function SharedMsg_$reflection() {
    return union_type("backwards-compability-with-1-0-4.Shared.SharedMsg", [], SharedMsg, () => [[]]);
}

export function init() {
    return [undefined, Command_none()];
}

export function update(msg, model) {
    return [model, Command_none()];
}

export function subscriptions(_model) {
    return empty();
}

