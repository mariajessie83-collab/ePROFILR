namespace SharedProject
{
    /// <summary>
    /// Represents the status of a case escalation
    /// </summary>
    public enum EscalationStatus
    {
        /// <summary>
        /// Initial state when a student is escalated to POD
        /// </summary>
        Active = 0,

        /// <summary>
        /// Pending review by POD (legacy status from old system)
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Call slip has been generated for the student
        /// </summary>
        Calling = 2,

        /// <summary>
        /// Student has arrived for the meeting
        /// </summary>
        Arrived = 3,

        /// <summary>
        /// Case has been resolved
        /// </summary>
        Resolved = 4,

        /// <summary>
        /// Case has been closed
        /// </summary>
        Closed = 5,

        /// <summary>
        /// Teacher has withdrawn/retracted the escalation
        /// </summary>
        Withdrawn = 6,

        /// <summary>
        /// Case record is being created or reviewed by POD
        /// </summary>
        UnderReview = 7,

        /// <summary>
        /// Parent conference has been scheduled, waiting for parent arrival
        /// </summary>
        ParentOnHold = 8
    }
}
