<h1># SGDiscordBot</h1>
Bot can currently be used to store server/user information in a database. Along with log messages.
Config .json files go in a folder called Data in the same path as the .exe

<h2>Commands:</h2>

<b>shutdown:</b>
Shuts the bot down. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

<b>restart:</b>
restarts the bot. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

<b>messagecount:</b>
Can get several message count statistics for the server.<br />

| Command					 | Description	 |
| -------------------------- | ------------- |
| @bot messagecount			 | Top message count on the server.							|
| @bot messagecount [number] | Top n message counts on the server.						|
| @bot messagecount @user	 | Will get a user's message count.							|
| @bot messagecount @role	 | Will get the message count for a role and it's top user. |

<b>reloadmessages:</b>
Will reload all the messages for the channel the messages was sent in. Or all on the server if all is passed in.<br />
Does not overwrite messages already in the database.<br />
Can be an expensive operation, only use if the bot has missed a significant amount of messages or when the bot is first added to server.<br />
Can only be used by a user with a role in commandRoleIds in CredConfig.json.<br />

| Command					 | Description	 |
| -------------------------- | ------------- |
| @bot reloadmessages		 | Reloads messages for channel message was sent in.		|
| @bot reloadmessages all	 | Reloads messages for every channel on server.			|

<b>emojicount:</b>

Can get various emoji use statistics for the server. <br />
Can only get custom emojis for the server. <br />
Note this can be a slow operation on server's with a large message count. <br />

| Command							 | Description	 |
| --------------------------		 | ------------- |
| @bot emojicount					 | Gets the top used emoji and top user for emoji.			|
| @bot emojicount [number]			 | Top n emoji counts.										|
| @bot emojicount [emoji]			 | Use count of specific emoji and its top user.			|
| @bot emojicount [user]			 | Get a user's most used emoji.							|
| @bot emojicount [number] [user]	 | Top n emoji counts for a specific user.					|


<b>rolecounts:</b>
Will get the counts of users in roles for the server.<br />
Use mentions is true/false and determines if the list will mention the roles or just give names.<br />
Can only be used by a user with a role in commandRoleIds in CredConfig.json.<br />

| Command					 				| Description	 									|
| -------------------------- 				| -------------  									|
| @bot rolecounts [useMentions]	 			| Outputs list to channel command was used in.		|
| @bot rolecounts [channel] [useMentions]	| Outputs list to given channel.					|