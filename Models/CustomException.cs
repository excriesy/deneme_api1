using System;

namespace ShareVault.API.Models
{
    public class CustomException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }
        public string UserMessage { get; }

        public CustomException(string message, int statusCode, string errorCode, string userMessage = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            UserMessage = userMessage ?? message;
        }

        public static CustomException Unauthorized(string message = "Yetkilendirme hatası")
        {
            return new CustomException(message, 401, "UNAUTHORIZED", "Oturum süreniz dolmuş olabilir. Lütfen tekrar giriş yapın.");
        }

        public static CustomException Forbidden(string message = "Erişim reddedildi")
        {
            return new CustomException(message, 403, "FORBIDDEN", "Bu işlem için yetkiniz bulunmuyor.");
        }

        public static CustomException NotFound(string message = "Kayıt bulunamadı")
        {
            return new CustomException(message, 404, "NOT_FOUND", "İstediğiniz kayıt bulunamadı.");
        }

        public static CustomException BadRequest(string message = "Geçersiz istek")
        {
            return new CustomException(message, 400, "BAD_REQUEST", "İstek formatı geçersiz.");
        }

        public static CustomException FileTooLarge(string message = "Dosya boyutu çok büyük")
        {
            return new CustomException(message, 400, "FILE_TOO_LARGE", "Dosya boyutu izin verilen limiti aşıyor.");
        }

        public static CustomException InvalidFileType(string message = "Geçersiz dosya türü")
        {
            return new CustomException(message, 400, "INVALID_FILE_TYPE", "Bu dosya türü desteklenmiyor.");
        }
    }
} 