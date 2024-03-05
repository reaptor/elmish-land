module ElmishLand.AppError

type AppError =
    | ProcessError of string
    | FsProjNotFound
    | MultipleFsProjFound
    | FsProjValidationError of string list
    | DotnetSdkNotFound
    | NodeNotFound
    | DepsMissingFromPaket
    | PaketNotInstalled
    | ViteNotInstalled
    | PagesDirectoryMissing
    | ElmishLandProjectMissing
