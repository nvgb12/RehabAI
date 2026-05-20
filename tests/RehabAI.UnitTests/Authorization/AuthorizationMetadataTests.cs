using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using RehabAI.Api.Authorization;
using RehabAI.Api.Controllers;

namespace RehabAI.UnitTests.Authorization;

public class AuthorizationMetadataTests
{
    [Fact]
    public void AuthController_IsExplicitlyPublic()
    {
        Assert.Contains(
            typeof(AuthController).GetCustomAttributes<AllowAnonymousAttribute>(true),
            _ => true);
    }

    [Theory]
    [InlineData(typeof(DoctorsController), nameof(DoctorsController.SearchDoctors))]
    [InlineData(typeof(DoctorsController), nameof(DoctorsController.GetDoctor))]
    [InlineData(typeof(DoctorsController), nameof(DoctorsController.GetAvailableSlots))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.BrowseProducts))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.GetProduct))]
    [InlineData(typeof(SubscriptionsController), nameof(SubscriptionsController.GetSubscriptionPlans))]
    public void PublicEndpoints_DoNotRequireAuthorizeMetadata(Type controllerType, string methodName)
    {
        Assert.Empty(GetAuthorizeAttributes(controllerType, methodName));
    }

    [Fact]
    public void AdminController_RequiresActiveAdminPolicy()
    {
        Assert.Contains(
            typeof(AdminController).GetCustomAttributes<AuthorizeAttribute>(true),
            attribute => attribute.Policy == AccessPolicies.ActiveAdmin);
    }

    [Theory]
    [InlineData(typeof(PatientsController), nameof(PatientsController.GetProfile))]
    [InlineData(typeof(PatientsController), nameof(PatientsController.UpdateProfile))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.CreateAppointment))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.GetAppointment))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.GetPatientAppointments))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.PlaceOrder))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.GetOrder))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.GetPatientOrders))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.GetMyOrders))]
    [InlineData(typeof(CommerceController), nameof(CommerceController.GetMyOrder))]
    [InlineData(typeof(SubscriptionsController), nameof(SubscriptionsController.GetMySubscription))]
    [InlineData(typeof(SubscriptionsController), nameof(SubscriptionsController.Subscribe))]
    [InlineData(typeof(SubscriptionsController), nameof(SubscriptionsController.ConfirmSubscriptionPayment))]
    public void PatientEndpoints_RequireActivePatientPolicy(Type controllerType, string methodName)
    {
        Assert.Contains(
            GetAuthorizeAttributes(controllerType, methodName),
            attribute => attribute.Policy == AccessPolicies.ActivePatient);
    }

    [Theory]
    [InlineData(nameof(DoctorsController.GetScheduleSlots))]
    [InlineData(nameof(DoctorsController.CreateScheduleSlot))]
    [InlineData(nameof(DoctorsController.UpdateScheduleSlot))]
    [InlineData(nameof(DoctorsController.DisableScheduleSlot))]
    [InlineData(nameof(DoctorsController.UploadCredential))]
    [InlineData(nameof(DoctorsController.ResendInvitation))]
    public void DoctorScheduleAndCredentialEndpoints_RequireDoctorStaffOrAdminPolicy(string methodName)
    {
        Assert.Contains(
            GetAuthorizeAttributes(typeof(DoctorsController), methodName),
            attribute => attribute.Policy == AccessPolicies.ActiveDoctorStaffOrAdmin);
    }

    private static IReadOnlyList<AuthorizeAttribute> GetAuthorizeAttributes(
        Type controllerType,
        string methodName)
    {
        var method = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == methodName);

        return controllerType
            .GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(method.GetCustomAttributes<AuthorizeAttribute>(true))
            .ToList();
    }
}
