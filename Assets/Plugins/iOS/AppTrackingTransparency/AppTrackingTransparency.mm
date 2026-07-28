#import <Foundation/Foundation.h>

#if __has_include(<AppTrackingTransparency/AppTrackingTransparency.h>)
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#endif
#import <AdSupport/AdSupport.h>

// Mirrors ATTrackingManagerAuthorizationStatus so the C# side doesn't need to
// link the framework's enum directly. NotAvailable (-1) covers iOS < 14,
// where ATT doesn't exist and tracking is governed by the old
// ASIdentifierManager.isAdvertisingTrackingEnabled flag instead.
typedef NS_ENUM(NSInteger, SHTrackingAuthorizationStatus) {
  SHTrackingAuthorizationStatusNotAvailable = -1,
  SHTrackingAuthorizationStatusNotDetermined = 0,
  SHTrackingAuthorizationStatusRestricted = 1,
  SHTrackingAuthorizationStatusDenied = 2,
  SHTrackingAuthorizationStatusAuthorized = 3,
};

static NSString *const kUnityCallbackGameObject = @"AppTrackingTransparencyBridge";
static NSString *const kUnityCallbackMethod = @"OnAuthorizationStatusReceived";

extern "C" {

int _ATT_GetAuthorizationStatus() {
#if __has_include(<AppTrackingTransparency/AppTrackingTransparency.h>)
  if (@available(iOS 14, *)) {
    return (int)[ATTrackingManager trackingAuthorizationStatus];
  }
#endif
  if ([[ASIdentifierManager sharedManager] isAdvertisingTrackingEnabled]) {
    return SHTrackingAuthorizationStatusAuthorized;
  }
  return SHTrackingAuthorizationStatusNotAvailable;
}

// Requests the ATT prompt (iOS 14+ only; a no-op on earlier versions since the
// dialog doesn't exist there). Result is delivered asynchronously to Unity via
// UnitySendMessage because the completion handler fires on an arbitrary queue
// on Apple's side, well after this function has already returned.
void _ATT_RequestAuthorization() {
#if __has_include(<AppTrackingTransparency/AppTrackingTransparency.h>)
  if (@available(iOS 14, *)) {
    [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
      dispatch_async(dispatch_get_main_queue(), ^{
        NSString *statusString = [NSString stringWithFormat:@"%ld", (long)status];
        UnitySendMessage([kUnityCallbackGameObject UTF8String],
                          [kUnityCallbackMethod UTF8String],
                          [statusString UTF8String]);
      });
    }];
    return;
  }
#endif
  dispatch_async(dispatch_get_main_queue(), ^{
    NSString *statusString = [NSString stringWithFormat:@"%ld", (long)_ATT_GetAuthorizationStatus()];
    UnitySendMessage([kUnityCallbackGameObject UTF8String],
                      [kUnityCallbackMethod UTF8String],
                      [statusString UTF8String]);
  });
}

} // extern "C"
