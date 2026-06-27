using FluentValidation;
using FluentValidation.Results;
using IdentityService.Api.Endpoints.SignUp;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentityService.UnitTests.Api.Endpoints;

public sealed class SignUpEndpointsTests
{
    [Fact]
    public async Task SignUpAsync_ReturnsCreated_WhenHandlerSucceeds()
    {
        var response = new SignUpResponse(Guid.NewGuid(), "a@b.c", "Avery", UserRoles.TenantAdmin, "Active");
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(SignUpResult.Success(response));

        var result = await SignUpEndpoints.SignUpAsync(
            ValidRequest(),
            new DefaultHttpContext(),
            sender.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        var created = Assert.IsType<Created<SignUpResponse>>(result);
        Assert.Equal($"/users/{response.UserId}", created.Location);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task SignUpAsync_ReturnsValidationProblem_WhenHandlerReturnsFailure()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(SignUpResult.Failure(new[]
              {
                  new AuthError("Email", "Email already exists.")
              }));

        var result = await SignUpEndpoints.SignUpAsync(
            ValidRequest(),
            new DefaultHttpContext(),
            sender.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task SignUpAsync_ReturnsValidationProblem_WhenValidationExceptionThrown()
    {
        var failures = new[]
        {
            new ValidationFailure("FullName", "Full name is required."),
            new ValidationFailure("Email", "Email is required.")
        };
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new ValidationException(failures));

        var result = await SignUpEndpoints.SignUpAsync(
            ValidRequest(),
            new DefaultHttpContext(),
            sender.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        var details = Assert.IsType<HttpValidationProblemDetails>(problem.ProblemDetails);
        Assert.True(details.Errors.ContainsKey("FullName"));
        Assert.True(details.Errors.ContainsKey("Email"));
    }

    private static SignUpRequest ValidRequest() =>
        new("Avery Nguyen", "avery@example.com", "Password1!", "Password1!", true);
}
