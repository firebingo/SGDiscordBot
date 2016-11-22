# SGDiscordBot
Bot can currently be used to store server/user information in a database. Along with log messages.
Config .json files go in a folder called Data in the same path as the .exe

Commands:

shutdown:
Shuts the bot down. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

restart:
restarts the bot. Can only be used by a user with a role in commandRoleIds in CredConfig.json.
ex. @bot shutdown

messagecount:
Can get several message count statistics for the server.
@bot messagecount 			Will get the top message count on the server. 
@bot messagecount [number]  Will get the top n message counts on the server.
@bot messagecount @user 	Will get a user's message count.
@bot messagecount @role 	Will get the message count for a role and it's top user.
