using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class RoutinesController : ControllerBase
    {
        [HttpGet]
        [Route("api/routines")]
        public ActionResult<Dictionary<string, object>> GetRoutinePlans()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetRoutinePlans(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines")]
        public ActionResult<Dictionary<string, object>> CreateRoutinePlan([FromBody] Dictionary<string, object> routineData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.CreateRoutinePlan(routineData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/routines/{routineId}")]
        public ActionResult<Dictionary<string, object>> UpdateRoutinePlan(long routineId, [FromBody] Dictionary<string, object> routineData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.UpdateRoutinePlan(routineId, routineData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/{routineId}")]
        public ActionResult<Dictionary<string, object>> DeleteRoutinePlan(long routineId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeleteRoutinePlan(routineId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Ejercicios ============================

        [HttpGet]
        [Route("api/routines/exercises")]
        public ActionResult<Dictionary<string, object>> GetExercises()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetExercises(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines/exercises")]
        public ActionResult<Dictionary<string, object>> CreateExercise([FromBody] Dictionary<string, object> exerciseData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.CreateExercise(exerciseData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/routines/exercises/{exerciseId}")]
        public ActionResult<Dictionary<string, object>> UpdateExercise(long exerciseId, [FromBody] Dictionary<string, object> exerciseData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.UpdateExercise(exerciseId, exerciseData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/exercises/{exerciseId}")]
        public ActionResult<Dictionary<string, object>> DeleteExercise(long exerciseId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeleteExercise(exerciseId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ===================== Ejercicios de una rutina =====================

        [HttpGet]
        [Route("api/routines/{planId}/exercises")]
        public ActionResult<Dictionary<string, object>> GetPlanExercises(long planId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetPlanExercises(planId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines/{planId}/exercises")]
        public ActionResult<Dictionary<string, object>> AddPlanExercise(long planId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.AddPlanExercise(planId, data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/routines/plan-exercises/{planExerciseId}")]
        public ActionResult<Dictionary<string, object>> UpdatePlanExercise(long planExerciseId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.UpdatePlanExercise(planExerciseId, data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/plan-exercises/{planExerciseId}")]
        public ActionResult<Dictionary<string, object>> DeletePlanExercise(long planExerciseId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeletePlanExercise(planExerciseId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Asignaciones ==========================

        [HttpGet]
        [Route("api/routines/assignments")]
        public ActionResult<Dictionary<string, object>> GetAssignments([FromQuery] long? memberId = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetAssignments(out var response, memberId);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines/assignments")]
        public ActionResult<Dictionary<string, object>> AssignPlan([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            long memberId = data.ContainsKey("memberId") ? Convert.ToInt64(data["memberId"]) : 0L;
            long planId = data.ContainsKey("planId") ? Convert.ToInt64(data["planId"]) : 0L;

            var success = DataCenter.BusinessProcess.Routines.AssignPlan(memberId, planId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/assignments/{assignmentId}")]
        public ActionResult<Dictionary<string, object>> DeactivateAssignment(long assignmentId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeactivateAssignment(assignmentId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Entrenamientos ========================

        [HttpGet]
        [Route("api/routines/workouts")]
        public ActionResult<Dictionary<string, object>> GetWorkouts(
            [FromQuery] long? memberId = null,
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? fromDate = DateTime.TryParse(from, out var parsedFrom) ? parsedFrom : null;
            DateTime? toDate = DateTime.TryParse(to, out var parsedTo) ? parsedTo : null;

            var success = DataCenter.BusinessProcess.Routines.GetWorkouts(out var response, memberId, fromDate, toDate, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines/workouts")]
        public ActionResult<Dictionary<string, object>> CreateWorkout([FromBody] Dictionary<string, object> workoutData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.CreateWorkout(workoutData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/workouts/{workoutId}")]
        public ActionResult<Dictionary<string, object>> DeleteWorkout(long workoutId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeleteWorkout(workoutId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Medidas ===============================

        [HttpGet]
        [Route("api/routines/measurements")]
        public ActionResult<Dictionary<string, object>> GetMeasurements(
            [FromQuery] long? memberId = null,
            [FromQuery] string? type = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetMeasurements(out var response, memberId, type);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines/measurements")]
        public ActionResult<Dictionary<string, object>> CreateMeasurement([FromBody] Dictionary<string, object> measurementData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.CreateMeasurement(measurementData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/measurements/{measurementId}")]
        public ActionResult<Dictionary<string, object>> DeleteMeasurement(long measurementId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeleteMeasurement(measurementId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
