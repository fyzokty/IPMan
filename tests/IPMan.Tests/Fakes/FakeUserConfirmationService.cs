using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

public sealed class FakeUserConfirmationService : IUserConfirmationService
{
    public List<UserConfirmationRequest> Requests { get; } = new();

    public Queue<bool> Answers { get; } = new();

    public bool Answer { get; set; }

    public bool Confirm(UserConfirmationRequest request)
    {
        Requests.Add(request);
        return Answers.Count > 0 ? Answers.Dequeue() : Answer;
    }
}
