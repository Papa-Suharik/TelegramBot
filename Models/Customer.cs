namespace PEPCHABUILD.Models;

public class Customer
{
    public string? UserName { get; }
    public long ChatId { get; }
    public int UserAssignedNumber { get; }
    public DateTime RegistrationDate { get; }
    public Customer(string userName, long chatId, int userAssNum, DateTime registrationDate)
    {
        UserName = userName;
        ChatId = chatId;
        UserAssignedNumber = userAssNum;
        RegistrationDate = registrationDate;
    }

    public override string ToString()
    {
        return  $"Customer: ChatId: '{ChatId}', UserName: '{UserName}', UserAssignedNumber: '{UserAssignedNumber}, DateTime: '{RegistrationDate}'";
    }
}