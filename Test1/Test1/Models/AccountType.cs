namespace Test1.Models
{
    public enum AccountType
    {
        /// <summary>
        /// Recurring payments for a specific period of time.
        /// </summary>
        TERM = 0,

        /// <summary>
        /// Membership for a specific period of time paid in advance (no recurring payments).
        /// </summary>
        PREPAID = 1,

        /// <summary>
        /// Recurring payments for an indefinite period of time.
        /// </summary>
        OPENEND = 2,

        /// <summary>
        /// No payment for membership.
        /// </summary>
        GUEST = 3
    }
}