using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<UserLoginAccount> UserLoginAccount { get; set; }
        public DbSet<UserVerificationDetails> UserVerificationDetails { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UserLoginSessionKeys> UserLoginSessionKeys { get; set; }
        public DbSet<PersonalityQuestionaire> PersonalityQuestionaire { get; set; }
        public DbSet<UserPersonalityQuestionaireDetails> UserPersonalityQuestionaireDetails { get; set; }
        public DbSet<Genders> Genders { get; set; }
        public DbSet<Countries> Countries { get; set; }
        public DbSet<ExternalAccountLogin> ExternalAccountLogin { get; set; }
        public DbSet<StateProvinces> StateProvinces { get; set; }
        public DbSet<SubscriptionType> SubscriptionType { get; set; }
        public DbSet<UserProfileSetting> UserProfileSetting { get; set; }
        public DbSet<UserFriendList> UserFriendList { get; set; }
        public DbSet<ChatMessages> ChatMessages { get; set; }
        public DbSet<UserChatGroup> UserChatGroup { get; set; }
        public DbSet<UserChatGroupFriends> UserChatGroupFriends { get; set; }
        public DbSet<UserChatGroupMessages> UserChatGroupMessages { get; set; }
        public DbSet<GooglePlaceTypes> GooglePlaceTypes { get; set; }
        public DbSet<UserInvitationDetails> UserInvitationDetails { get; set; }
        public DbSet<GroupPoll> GroupPoll { get; set; }
        public DbSet<UserCalendarEvent> UserCalendarEvent { get; set; }
        public DbSet<UserCardInfo> UserCardInfo { get; set; }
        public DbSet<SquareCustomerDetails> SquareCustomerDetails { get; set; }
        public DbSet<SquareCustomerCardDetails> SquareCustomerCardDetails { get; set; }
        public DbSet<Merchants> Merchants { get; set; }
        public DbSet<MerchantLoginSessionKeys> MerchantLoginSessionKeys { get; set; }
        public DbSet<MerchantCampaigns> MerchantCampaigns { get; set; }
        public DbSet<MerchantPackages> MerchantPackages { get; set; }
        public DbSet<MerchantVerificationDetails> MerchantVerificationDetails { get; set; }
        public DbSet<PackageType> PackageType { get; set; }
        public DbSet<MerchantPackagesPictures> MerchantPackagesPictures { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<BookingStages> BookingStages { get; set; }
        public DbSet<PaymentStages> PaymentStages { get; set; }
        public DbSet<Events> Events { get; set; }
        public DbSet<PaymentType> PaymentType { get; set; }
        public DbSet<PaymentCategory> PaymentCategory { get; set; }
        public DbSet<UserPaymentHistory> UserPaymentHistory { get; set; }
        public DbSet<TransactionType> TransactionType { get; set; }
        public DbSet<CostType> CostType { get; set; }
        public DbSet<UserTransactions> UserTransactions { get; set; }
        public DbSet<PaymentErrorHistory> PaymentErrorHistory { get; set; }
        public DbSet<DailyPNL> DailyPNL { get; set; }
        public DbSet<UserProfileImages> UserProfileImages { get; set; }
        public DbSet<UserSMSInvitationDetails> UserSMSInvitationDetails { get; set; }
        public DbSet<Jokes> Jokes { get; set; }
        public DbSet<EventParticipateUsers> EventParticipateUsers { get; set; }
        public DbSet<MuteChatGroupNotificationDetails> MuteChatGroupNotificationDetails { get; set; }
        public DbSet<MuteChatNotificationDetails> MuteChatNotificationDetails { get; set; }
        public DbSet<UserPersonalitySummary> UserPersonalitySummary { get; set; }
        public DbSet<Activities> Activities { get; set; }
        public DbSet<UserActivities> UserActivities { get; set; }
        public DbSet<MBTIPersonalities> MBTIPersonalities { get; set; }
        public DbSet<MerchantTypes> MerchantTypes { get; set; }
        public DbSet<ChatGroups> ChatGroups { get; set; }
        public DbSet<UsersWaitList> UsersWaitList { get; set; }
        public DbSet<ReportedUsers> ReportedUsers { get; set; }
    }
}