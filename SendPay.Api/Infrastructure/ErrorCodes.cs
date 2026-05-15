namespace SendPay.Api.Infrastructure;

/// <summary>
/// T\u1eadp trung m\u00e3 l\u1ed7i d\u00f9ng cho to\u00e0n h\u1ec7 th\u1ed1ng. M\u1ed7i m\u00e3 l\u00e0 m\u1ed9t key \u1ed5n \u0111\u1ecbnh \u0111\u01b0\u1ee3c d\u1ecbch \u1edf frontend
/// (xem <c>SendPay.Web/Services/Translations.cs</c>).
/// </summary>
public static class ErrorCodes
{
    public const string System            = "error.system";
    public const string Unauthorized      = "error.unauthorized";
    public const string Forbidden         = "error.forbidden";
    public const string NotFound          = "error.not_found";
    public const string BadRequest        = "error.bad_request";
    public const string RateLimitAuth     = "error.rate_limit.auth";

    public const string UserNotFound      = "error.user.not_found";
    public const string EmailInUse        = "error.user.email_in_use";
    public const string PhoneInUse        = "error.user.phone_in_use";
    public const string CurrentPasswordWrong = "error.user.current_password_wrong";
    public const string PasswordWeak      = "error.password.weak";

    public const string ProfileUrlInvalid = "error.profile.url_invalid";

    public const string AuthInvalidCredentials = "error.auth.invalid_credentials";
    public const string AuthAccountLocked      = "error.auth.account_locked";
    public const string AuthRefreshInvalid     = "error.auth.refresh_invalid";
    public const string AuthTokenRevoked       = "error.auth.token_revoked";

    public const string TopUpDisabled     = "error.topup.disabled";
    public const string TopUpRefCollision = "error.topup.ref_collision";

    public const string TransferSenderNotFound    = "error.transfer.sender_not_found";
    public const string TransferReceiverNotFound  = "error.transfer.receiver_not_found";
    public const string TransferSelf              = "error.transfer.self";
    public const string TransferInsufficient      = "error.transfer.insufficient_balance";
    public const string TransferRace              = "error.transfer.race";
    public const string TransferNotFound          = "error.transfer.not_found";

    public const string RecipientNotFound         = "error.recipient.not_found";
    public const string RecipientNameRequired     = "error.recipient.name_required";
    public const string RecipientBankRequired     = "error.recipient.bank_required";
    public const string RecipientHolderRequired   = "error.recipient.holder_required";
    public const string RecipientAccountRequired  = "error.recipient.account_required";
    public const string RecipientAccountInvalid   = "error.recipient.account_invalid";
    public const string RecipientBankNotInCatalog = "error.recipient.bank_not_in_catalog";
    public const string RecipientSwiftMissing     = "error.recipient.swift_missing";

    public const string BankLinkInvalidBank    = "error.bank_link.invalid_bank";
    public const string BankLinkStateMissing   = "error.bank_link.state_missing";
    public const string BankLinkStateInvalid   = "error.bank_link.state_invalid";
    public const string BankLinkSessionUsed    = "error.bank_link.session_used";
    public const string BankLinkSessionExpired = "error.bank_link.session_expired";

    public const string AdminCannotLockAdmin       = "error.admin.cannot_lock_admin";
    public const string AdminCannotSelfLock        = "error.admin.cannot_self_lock";
    public const string AdminCannotSelfRevoke      = "error.admin.cannot_self_revoke";
    public const string AdminLastAdmin             = "error.admin.last_admin";
    public const string AdminCannotDeleteAdmin     = "error.admin.cannot_delete_admin";
    public const string AdminImportEmpty           = "error.admin.import_empty";
}
