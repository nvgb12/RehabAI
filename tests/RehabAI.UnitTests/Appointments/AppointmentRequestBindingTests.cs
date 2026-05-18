using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Appointments;
using RehabAI.Api.Controllers;

namespace RehabAI.UnitTests.Appointments;

public class AppointmentRequestBindingTests
{
    [Fact]
    public void CreateAppointmentRequest_UsesDirectGuidBodyFields()
    {
        Assert.Equal(typeof(Guid), GetPropertyType(nameof(CreateAppointmentRequest.PatientProfileId)));
        Assert.Equal(typeof(Guid), GetPropertyType(nameof(CreateAppointmentRequest.DoctorProfileId)));
        Assert.Equal(typeof(Guid), GetPropertyType(nameof(CreateAppointmentRequest.MedicalServiceId)));
        Assert.Equal(typeof(Guid), GetPropertyType(nameof(CreateAppointmentRequest.ScheduleSlotId)));
        Assert.Equal(typeof(string), GetPropertyType(nameof(CreateAppointmentRequest.Reason)));
    }

    [Fact]
    public void CreateAppointment_BindsRequestFromBodyDirectly()
    {
        var method = typeof(AppointmentsController).GetMethod(nameof(AppointmentsController.CreateAppointment));
        var requestParameter = method!.GetParameters()
            .Single(parameter => parameter.ParameterType == typeof(CreateAppointmentRequest));

        Assert.NotNull(requestParameter.GetCustomAttribute<FromBodyAttribute>());
    }

    private static Type GetPropertyType(string propertyName)
    {
        return typeof(CreateAppointmentRequest).GetProperty(propertyName)!.PropertyType;
    }
}
