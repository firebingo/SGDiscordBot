<h1># SGDiscordBot</h1>
Bot can currently be used to store server/user information in a database. Along with log messages.
Config .json files go in a folder called Data in the same path as the .exe

<h2>Commands</h2>:

<b>shutdown</b>:
Shuts the bot down. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

<b>restart</b>:
restarts the bot. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

<b>messagecount</b>:
Can get several message count statistics for the server.<br />

| Command                    | Description   |
| -------------------------- | ------------- |
| @bot messagecount          |Top message count on the server.                         |
| @bot messagecount [number] |Top n message counts on the server.                      | 
| @bot messagecount @user    |Will get a user's message count.                         | 
| @bot messagecount @role    |Will get the message count for a role and it's top user. |

<b>reloadmessages</b>:
Will reload all the messages for the channel the messages was sent in. Or all on the server if all is passed in.
Does not overwrite messages already in the database.
Can be an expensive operation, only use if the bot has missed a significant amount of messages or when the bot is first added to server.
Can only be used by a user with a role in commandRoleIds in CredConfig.json.

| Command                    | Description   |
| -------------------------- | ------------- |
| @bot reloadmessages        |Reloads messages for channel message was sent in.        |
| @bot messagecount all      |Reloads messages for every channel on server.            | 