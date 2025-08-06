import { Union } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Types.js";
import { union_type, unit_type } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Reflection.js";
import { singleton } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/AsyncBuilder.js";
import { sleep as sleep_1 } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Async.js";
import { some } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Option.js";
import { Command_ofCmd, Command_none, Command_ofShared } from "../.elmish-land/Base/Command.fs.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_perform } from "../.elmish-land/App/fable_modules/Fable.Elmish.4.0.0/./cmd.fs.js";
import { empty } from "../.elmish-land/App/fable_modules/fable-library-js.4.25.0/List.js";

export class SharedMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["DoSleep", "SleepCompleted"];
    }
}

export function SharedMsg_$reflection() {
    return union_type("using-commands-from-shared-module-does-not-work-27.Shared.SharedMsg", [], SharedMsg, () => [[], [["Item", unit_type]]]);
}

export function sleep() {
    return singleton.Delay(() => singleton.Bind(sleep_1(1000), () => {
        console.log(some("Slept for 1 second"));
        return singleton.Return(undefined);
    }));
}

export function init() {
    return [undefined, Command_ofShared(new SharedMsg(0, []))];
}

export function update(msg, model) {
    if (msg.tag === 1) {
        return [model, Command_none()];
    }
    else {
        return [model, Command_ofCmd(Cmd_OfAsyncWith_perform((x) => {
            Cmd_OfAsync_start(x);
        }, sleep, undefined, () => (new SharedMsg(1, [undefined]))))];
    }
}

export function subscriptions(_model) {
    return empty();
}

