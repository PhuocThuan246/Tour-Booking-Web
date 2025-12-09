using System.Net.Mail;
using System.Net;

namespace TourBookingWeb.Services
{
    public class EmailService
    {
        public static void SendTokenEmail(string toEmail, string token)
        {
            var fromAddress = new MailAddress("ptduong393@gmail.com", "TourBookingWeb");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "dastskzojwjwjfrq";
            const string subject = "🔐 Mã xác thực tài khoản";

            string body = $@"
<table style='width:100%; background-color:#f4f4f4; padding:40px 0; font-family:Segoe UI,Roboto,sans-serif;'>
    <tr>
        <td align='center'>
            <table style='width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.05);'>
                <tr>
                    <td style='background-color:#0069d9; padding:30px 20px; text-align:center; color:white;'>
                        <h1 style='margin:0; font-size:24px;'>🌍 TourBookingWeb</h1>
                        <p style='margin:5px 0 0; font-size:16px;'>Xác thực tài khoản của bạn</p>
                    </td>
                </tr>
                <tr>
                    <td style='padding:30px; color:#333; font-size:16px;'>
                        <p>Xin chào,</p>
                        <p>Cảm ơn bạn đã đăng ký tại <strong>TourBookingWeb</strong>.</p>
                        <p style='margin-bottom: 20px;'>Dưới đây là mã xác thực tài khoản của bạn:</p>
                        <div style='text-align:center; margin:30px 0;'>
                            <span style='display:inline-block; background-color:#eef6ff; padding:18px 36px; font-size:32px; font-weight:bold; color:#0069d9; letter-spacing:5px; border-radius:12px;'>
                                {token}
                            </span>
                        </div>
                        <p style='color:#777;'>Mã có hiệu lực trong vòng <strong>30 phút</strong>.</p>
                        <p>Nếu bạn không thực hiện hành động này, vui lòng bỏ qua email này.</p>
                        <p style='margin-top:40px;'>Trân trọng,<br><strong>Đội ngũ TourBookingWeb</strong></p>
                    </td>
                </tr>
                <tr>
                    <td style='background-color:#f8f8f8; text-align:center; padding:15px 20px; font-size:12px; color:#aaa;'>
                        © {DateTime.Now.Year} TourBookingWeb. All rights reserved.
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>";



            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }


    }
}
