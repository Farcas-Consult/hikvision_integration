


we need to implement a sync service. it will get gym members from our gym system, and sync them with hikvision readers
below is a sample js script that gets members, i will provide the urls/envs

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "profilePictureUrl": "https://...",
  "phoneNumber": "+2547...",
  "membershipStatus": "active",
  "turnstileId": "TS-12345",
  "memberId": "gm_abc123",
  "isActive": true,
  "gender": "M",
  "Valid": {
    "enable": true,
    "beginTime": "2026-01-01T23:59:59",
    "endTime": "2030-01-01T23:59:59"
  },
  "lastUpdated": "2026-02-01T10:30:00.000Z"
} - this is the json you will receive from the server.


  async fetchGymMembers(): Promise<GymMember[]> {
    logger.info('Fetching gym members data...');

    const headers: Record<string, string> = {};
    if (config.gym.apiKey) {
      headers['x-api-key'] = config.gym.apiKey;
    }

    const response = await fetch(config.gym.apiUrl, {
      method: 'GET',
      headers,
      agent: config.gym.apiUrl.startsWith('https:') ? httpsAgent : undefined,
    });

    if (!response.ok) {
      const errorMsg = `Gym API failed: ${response.status} ${response.statusText}`;

      if (response.status === 401 || response.status === 403) {
        telegramService.incrementAuthFailures();
        await telegramService.notify(
          'auth_error',
          `<b>Authentication Error</b>\n\n` +
            `<b>Status:</b> ${response.status} ${response.statusText}\n` +
            `<b>URL:</b> ${config.gym.apiUrl}\n` +
            `<b>Failures:</b> ${telegramService.getAuthFailures()}\n\n` +
            `Please check API credentials and server status.`,
          'auth_failure'
        );
      }

      throw new Error(errorMsg);
    }

    const apiResponse = (await response.json()) as GymApiResponse;

    if (!apiResponse.success || !Array.isArray(apiResponse.data)) {
      const msg = `Gym API response invalid format: ${JSON.stringify(apiResponse)}`;
      logger.error(msg);
      console.error('Gym API response:', JSON.stringify(apiResponse, null, 2));

      await telegramService.notify(
        'api_error',
        `<b>API Response Error</b>\n\n` +
          `<b>Issue:</b> Invalid response format\n` +
          `<b>URL:</b> ${config.gym.apiUrl}\n\n` +
          `API returned unexpected data structure.`,
        'invalid_response'
      );

      throw new Error(msg);
    }

    logger.info(`Found ${apiResponse.data.length} members in gym system`);

    if (telegramService.getAuthFailures() > 0) {
      telegramService.resetAuthFailures();
      await telegramService.notify(
        'recovery',
        `<b>Connection Restored</b>\n\n` +
          `<b>Service:</b> Gym API\n` +
          `<b>Members Found:</b> ${apiResponse.data.length}\n\n` +
          `System is back online and functioning normally.`
      );
    }

    return apiResponse.data;
  }



  /ISAPI/AccessControl/UserInfo/SetUp?format=json

  hikvision readers are on this api:
  after we get the members, we shall put them, that is either update or create them. we need to do this effeciently, so that we only update members who have changed.

{
  "UserInfo": {
    "employeeNo": "10001",
    "name": "API Test User",
    "userType": "normal",
    "Valid": {
      "enable": false,
      "beginTime": "2026-01-01T23:59:59",
      "endTime": "2030-01-01T23:59:59"
    }
  }
}


200
// {
//     "statusCode": 1,
//     "statusString": "OK",
//     "subStatusCode": "ok"
// }

// 400
// {
//     "statusCode": 1,
//     "statusString": "OK",
//     "subStatusCode": "ok"
// }