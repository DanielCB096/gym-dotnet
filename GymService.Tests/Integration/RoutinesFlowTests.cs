using GymService.Modules.Members;
using GymService.Modules.Routines;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class RoutinesFlowTests
{
    private readonly DatabaseFixture _fx;

    public RoutinesFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Plans_Exercises_Assignments_Workouts_And_Measurements_Work_Together()
    {
        if (!_fx.Available) return;

        var routines = new RoutinesModule(_fx.Tools!);
        string planName = $"Plan_{Guid.NewGuid():N}"[..20];
        string exerciseName = $"Ejercicio_{Guid.NewGuid():N}"[..20];

        CurrentUser.Id = 1;

        try
        {
            // Plan y ejercicio
            Assert.True(routines.CreateRoutinePlan(new Dictionary<string, object>
            {
                ["planName"] = planName,
                ["description"] = "Prueba",
                ["difficulty"] = "intermediate",
                ["durationMinutes"] = 45
            }, out var planResponse), planResponse.GetValueOrDefault("message")?.ToString());

            long planId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_ROUTINE_PLANS WHERE PLAN_NAME = @n", ("@n", planName)));

            Assert.True(routines.CreateExercise(new Dictionary<string, object>
            {
                ["exerciseName"] = exerciseName,
                ["description"] = "Prueba",
                ["muscleGroup"] = "Pecho",
                ["equipment"] = "Mancuernas"
            }, out var exerciseResponse), exerciseResponse.GetValueOrDefault("message")?.ToString());

            long exerciseId = Convert.ToInt64(exerciseResponse["exerciseId"]);

            // Ejercicio dentro del plan
            Assert.True(routines.AddPlanExercise(planId, new Dictionary<string, object>
            {
                ["exerciseId"] = exerciseId,
                ["dayNumber"] = 1,
                ["sortOrder"] = 1,
                ["sets"] = 3,
                ["reps"] = 12,
                ["notes"] = "Calentar antes"
            }, out var addResponse), addResponse.GetValueOrDefault("message")?.ToString());

            // Duplicado en el mismo día
            Assert.False(routines.AddPlanExercise(planId, new Dictionary<string, object>
            {
                ["exerciseId"] = exerciseId,
                ["dayNumber"] = 1
            }, out var duplicate));

            Assert.True(routines.GetPlanExercises(planId, out var planExercises));
            var planExerciseRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(planExercises["data"]);
            var planExercise = Assert.Single(planExerciseRows);
            long planExerciseId = Convert.ToInt64(planExercise["id"]);
            Assert.Equal(12, Convert.ToInt32(planExercise["reps"]));

            Assert.True(routines.UpdatePlanExercise(planExerciseId, new Dictionary<string, object> { ["sets"] = 4 }, out _));
            Assert.Equal(4, Convert.ToInt32(_fx.Scalar("SELECT SETS FROM GYM_ROUTINE_PLAN_EXERCISES WHERE ID = @id", ("@id", planExerciseId))));

            // Asignación a socio
            long memberId = CreateMember();

            Assert.True(routines.AssignPlan(memberId, planId, out var assignmentResponse), assignmentResponse.GetValueOrDefault("message")?.ToString());
            long assignmentId = Convert.ToInt64(assignmentResponse["assignmentId"]);

            Assert.True(routines.GetAssignments(out var assignments, memberId));
            var assignmentRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(assignments["data"]);
            var assignment = Assert.Single(assignmentRows);
            Assert.True(Convert.ToBoolean(assignment["isActive"]));
            Assert.Equal(planName, assignment["planName"]);

            // Entrenamiento
            Assert.True(routines.CreateWorkout(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["exerciseId"] = exerciseId,
                ["sets"] = 3,
                ["reps"] = 10,
                ["weight"] = 20m,
                ["durationMinutes"] = 15,
                ["workoutDate"] = DateTime.Now.ToString("o")
            }, out var workoutResponse), workoutResponse.GetValueOrDefault("message")?.ToString());

            long workoutId = Convert.ToInt64(workoutResponse["workoutId"]);

            Assert.True(routines.GetWorkouts(out var workouts, memberId));
            var workoutRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(workouts["data"]);
            Assert.Single(workoutRows);
            Assert.Equal(exerciseName, workoutRows[0]["exerciseName"]);

            // Medida
            Assert.True(routines.CreateMeasurement(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["measurementType"] = "weight",
                ["value"] = 80.5m,
                ["unit"] = "kg",
                ["measuredAt"] = DateTime.Now.ToString("o")
            }, out var measurementResponse), measurementResponse.GetValueOrDefault("message")?.ToString());

            long measurementId = Convert.ToInt64(measurementResponse["measurementId"]);

            Assert.True(routines.GetMeasurements(out var measurements, memberId));
            var measurementRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(measurements["data"]);
            Assert.Single(measurementRows);
            Assert.Equal(80.5m, Convert.ToDecimal(measurementRows[0]["value"]));

            // No se puede borrar un plan con ejercicios/asignaciones.
            Assert.False(routines.DeleteRoutinePlan(planId, out var deletePlanResponse));
            Assert.Contains("historial", deletePlanResponse.GetValueOrDefault("message")?.ToString() ?? "");

            // Limpieza de referencias y borrado de ejercicio.
            Assert.True(routines.DeleteWorkout(workoutId, out _));
            Assert.True(routines.DeleteMeasurement(measurementId, out _));
            Assert.True(routines.DeactivateAssignment(assignmentId, out _));
            Assert.True(routines.DeletePlanExercise(planExerciseId, out _));
            Assert.True(routines.DeleteExercise(exerciseId, out _));

            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_EXERCISES WHERE ID = @id", ("@id", exerciseId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    private long CreateMember()
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"routine_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Rutina",
            ["lastName"] = "Test",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }
}
