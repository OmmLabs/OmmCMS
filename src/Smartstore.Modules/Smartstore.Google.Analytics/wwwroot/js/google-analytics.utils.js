(function ($, window, document, undefined) {

    $(function () {
        // Event handler to transmit updated consent after CMP initialization or if new consent by user.
        $(document).on("cmpOnInitialized cmpOnConsented", function (e, consentData) {
            gtag('consent', 'update', {
                'ad_storage': consentData.AllowAnalytics ? "granted" : "denied",
                'analytics_storage': consentData.AllowAnalytics ? "granted" : "denied",
                'ad_user_data': consentData.AdUserDataConsent ? "granted" : "denied",
                'ad_personalization': consentData.AdPersonalizationConsent ? "granted" : "denied",
            });
        });
    });

})(jQuery, this, document);