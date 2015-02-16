from ITable import *

class IndirectTable(ITable):
    """ A wrapper class for tables that forwards function calls to another table. """

    def __init__(self):
        pass

    def get_table(self):
        """ Gets the indirect table's underlying table.
            This abstract method is protected. """
        raise NotImplementedError("Method 'IndirectTable.get_table' was not implemented.")

    def insert(self, Value):
        """ Inserts an item into the table. """
        # Post:
        # Returns true if item is successfully inserted, false if the table already contains an item with the same search key.
        return self.get_table().insert(Value)

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self.get_table().contains_key(Key)

    def remove(self, Key):
        """ Removes a key from the table. """
        # Post:
        # This method returns true if the key is in the table, false if not.
        return self.get_table().remove(Key)

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.get_table().__iter__()

    def to_list(self):
        """ Gets the table's items as a read-only list.
            The elements in this list are in the same order as those in the table's iterator, obtained through '__iter__' (the get iterator method).
            Any statement that applies to this method therefore also applies to the '__iter__' (get iterator) method, and vice-versa. """
        # Post:
        # This method returns a read-only list that describes the items in this table.
        # Modifications to this list are not allowed - it is read-only.
        # Furthermore, this list may be an alias to an internal list containing the table's items, or a copy.
        # This list need not be sorted, but must contain every item in the table.
        return self.get_table().to_list()

    @property
    def key_map(self):
        """ Gets the table's record-to-key map. """
        return self.get_table().key_map

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        # Pre:
        # For this method to return an item in the table, rather than null, the key must be in the table, i.e.
        # ContainsKey(Key) must return true.
        # Post:
        # The return value of this method will be the item that corresponds with the key, or None, if it is not found.
        # It is recommended to check if the table contains the key by using ContainsKey.
        return self.get_table()[Key]

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.get_table().count