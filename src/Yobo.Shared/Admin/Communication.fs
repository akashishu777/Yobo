module Yobo.Shared.Admin.Communication

open System
open Yobo.Shared.Domain
open Domain
open Yobo.Shared.Communication

let routeBuilder _ m = sprintf "/api/admin/%s" m

type API = {
    GetAllUsers : SecuredParam<unit> -> ServerResponse<User list>
    AddCredits : SecuredParam<AddCredits> -> ServerResponse<unit>
    GetLessonsForDateRange : SecuredParam<DateTimeOffset * DateTimeOffset> -> ServerResponse<Lesson list>
    AddLessons : SecuredParam<AddLesson list> -> ServerResponse<unit>
    CancelLesson : SecuredParam<Guid> -> ServerResponse<unit>
} 