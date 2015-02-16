from ArrayList import *
from ISortableTable import *

class SortedTable(ISortableTable):
    """ A sortable table implementation that provides access to sorted objects which are sorted lazily. """

    def __init__(self, Table, Sorter):
        """ Creates a lazily sorted table with the specified table as backing storage, and the specified list sorter for sorting functionality. """
        self.sorted_list = None
        self.table = Table
        self.sorter = Sorter

    def sort_table(self):
        """ Writes a sorted list of the table's items to 'sortedList'.
            This method is private. """
        if self.sorted_list is None:
            targetList = ArrayList()
            for item in self.table:
                targetList.add(item)
            self.sorted_list = self.sorter.sort(targetList)

    def sort(self, Sorter):
        """ 'Sorts' the sorted table with the specified list sorter.
            The actual sorting process is deferred until __iter__ or ToList are called, however. """
        # Pre:
        # The table must not be empty for this method to change the table's state.
        # Post:
        # The table's items will be sorted.
        # After sorting, the table's 'ToList()' method must return a list whose items are sorted.
        # If the table's state is modified after 'Sort()' is called, the list produced by 'ToList()' need no longer be sorted.
        self.sorter = Sorter
        self.sorted_list = None

    def insert(self, Item):
        """ Inserts an item into the table. """
        # Post:
        # Returns true if item is successfully inserted, false if the table already contains an item with the same search key.
        self.sorted_list = None
        return self.table.insert(Item)

    def remove(self, Key):
        """ Removes a key from the table. """
        # Post:
        # This method returns true if the key is in the table, false if not.
        self.sorted_list = None
        return self.table.remove(Key)

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self.table.contains_key(Key)

    def to_list(self):
        """ Gets the table's items as a read-only list.
            The elements in this list are in the same order as those in the table's iterator, obtained through '__iter__' (the get iterator method).
            Any statement that applies to this method therefore also applies to the '__iter__' (get iterator) method, and vice-versa. """
        # Post:
        # This method returns a read-only list that describes the items in this table.
        # Modifications to this list are not allowed - it is read-only.
        # Furthermore, this list may be an alias to an internal list containing the table's items, or a copy.
        # This list need not be sorted, but must contain every item in the table.
        self.sort_table()
        return self.sorted_list

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.to_list().__iter__()

    @property
    def sorter(self):
        """ Gets the sorted table's list sorter. """
        return self.sorter_value

    @sorter.setter
    def sorter(self, value):
        """ Sets the sorted table's list sorter.
            This accessor is private. """
        self.sorter_value = value

    @property
    def key_map(self):
        """ Gets the table's record-to-key map. """
        return self.table.key_map

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.table.count

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        # Pre:
        # For this method to return an item in the table, rather than null, the key must be in the table, i.e.
        # ContainsKey(Key) must return true.
        # Post:
        # The return value of this method will be the item that corresponds with the key, or None, if it is not found.
        # It is recommended to check if the table contains the key by using ContainsKey.
        return self.table[Key]