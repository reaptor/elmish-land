module ElmishLand.Help

open ElmishLand.Base

let help () =
    printfn
        $"""
    Welcome to %s{appTitle}! (v%s{version})
    ⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺
    Here are the available commands:

    %s{cliName} init <project-name> ............ create a new project
    %s{cliName} server <working-directory> ... run a local dev server
    %s{cliName} build ................. build your app for production
    %s{cliName} add page <url> ....................... add a new page
    %s{cliName} add layout <name> .................. add a new layout
    %s{cliName} routes .................. list all routes in your app

    Want to learn more? Visit https://elmish.land/guide
    """
