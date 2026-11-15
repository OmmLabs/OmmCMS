# Phase 9 — Public account and authentication dependency extraction

Base: Phase 8 (`cms-public-commerce-extraction`).

- Physically extract eight already-blocked public account actions: Orders, CancelRecurringPayment, ReturnCases, DownloadableProducts, UserAgreement, RewardPoints, StockSubscriptions and StockSubscriptionsPOST; delete their dedicated models and seven Razor views.
- Remove unused order, payment, shipping, download, currency, product URL, reward and cart dependencies from CustomerController. Preserve profile, username, address, avatar and newsletter behavior, including the existing VAT compatibility path.
- Remove cart migration and registration reward dependencies from IdentityController; remove login guest-checkout flag and UI. Preserve login, registration, external authentication, events and recovery.
- Simplify MyAccountMenu to profile, addresses, password and avatar. Remove preferred shipping/payment fields from the public account editor and stop activating quick-checkout address mapping.
- Delete the obsolete analytics checkout filter and the DevTools checkout demonstration controller/filter/view. Retain the general modules.
- Keep the denylist, remaining admin commerce surfaces, shared model/service contracts, database entities and migrations until their remaining callers are extracted. No database or production changes.

CI checks assembly and constructor absence, source/publish absence, authenticated profile/address rendering, former account HTTP 404s, guest checkout absence, and previous CMS installation/editorial regressions.
