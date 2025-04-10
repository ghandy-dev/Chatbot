namespace Database.Entities

type Reminder = {
    reminder_id: int
    timestamp: string
    from_user_id: int
    from_username: string
    target_user_id: int
    target_username: string
    message: string
    reminded: int
}

type TimedReminder = {
    reminder_id: int
    timestamp: string
    from_user_id: int
    from_username: string
    target_user_id: int
    target_username: string
    message: string
    channel: string
    reminder_timestamp: string
    reminded: int
}
