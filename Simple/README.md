# Simple
A simple SQL and SQLite ORM that maps back to your class or can return dictionaries. Simple and flexible

Simple makes it easier to interact with SQL and SQLite by managing connections for each call and uses generics to enable mapping back
to the models your database layer represents.

#ExecuteReader and ExecuteReaderAsync
There are a few overloads to ExecuteReader allowing extra configuration of the call type (stored proc or text), a string for the query.

If you use the generic ExecuteReader<T>, it will return a list of objects of type T, populated with data mapped to the public properties in the class T.
If you use the other, you will get a list of Dictionary<string, object> items filled with data. The key values will be the column names from the query.

#ExecuteCommand and ExecuteCommandAsync
Allows the transactional execution of a SQL or SQLite command, similar to the SQLCommand object

#Connections
Define your connection in the web or app .config file;
  
Happy Coding!
