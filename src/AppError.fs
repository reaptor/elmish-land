module ElmishLand.AppError

type AppError =
    | ProcessError of string
    | FsProjNotFound
    | MultipleFsProjFound
    | FsProjValidationError of string list
    | DotnetSdkNotFound
    | NodeNotFound
    | ViteNotInstalled
    | PagesDirectoryMissing
    | ElmishLandProjectMissing
    | InvalidSettings of string
    | MissingMainLayout
