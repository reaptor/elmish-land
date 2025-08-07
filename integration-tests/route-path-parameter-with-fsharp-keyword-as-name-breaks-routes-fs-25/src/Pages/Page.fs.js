import { Union } from "../../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Types.js";
import { Msg_$reflection as Msg_$reflection_1 } from "./Layout.fs.js";
import { union_type } from "../../.elmish-land/App/fable_modules/fable-library-js.4.25.0/Reflection.js";
import { Command_none } from "../../.elmish-land/Base/Command.fs.js";
import { Page_from } from "../../.elmish-land/Base/Page.fs.js";

export class Msg extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["LayoutMsg"];
    }
}

export function Msg_$reflection() {
    return union_type("route-path-parameter-with-fsharp-keyword-as-name-breaks-routes-fs-25.Pages.Page.Msg", [], Msg, () => [[["Item", Msg_$reflection_1()]]]);
}

export function init() {
    return [undefined, Command_none()];
}

export function update(msg, model) {
    return [model, Command_none()];
}

export function view(_model, _dispatch) {
    return " Page";
}

export function page(_shared, _route) {
    return Page_from(init, (msg, model) => update(msg, undefined), (_model, _dispatch) => view(undefined, _dispatch), undefined, (Item) => (new Msg(Item)));
}

