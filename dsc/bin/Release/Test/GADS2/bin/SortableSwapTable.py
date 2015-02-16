from IndirectTable import *
from ISortableTable import *

class SortableSwapTable(IndirectTable, ISortableTable):
    """ A wrapper sortable table that allows for the underlying sortable table to be swapped out for another sortable table. """

    def __init__(self, table):
        """ Creates a new instance of a swap table. """
        IndirectTable.__init__(self)
        self.table = table

    def get_table(self):
        """ Gets the indirect table's underlying table.
            This method is protected. """
        return self.table

    def swap(self, Table):
        """ Changes the underlying sortable table implementation to the provided table. """
        # Post:
        # The underlying implementation of this table will be changed to 'Table', which will be populated with the items from the previous underlying table, in addition to the elements that were already in 'Table'.
        for item in self:
            Table.insert(item)
        self.table = Table

    def sort(self, Sorter):
        """ Sorts the table's contents in ascending order based on the the provided comparer. """
        # Pre:
        # The table must not be empty for this method to change the table's state.
        # Post:
        # The table's items will be sorted.
        # After sorting, the table's 'ToList()' method must return a list whose items are sorted.
        # If the table's state is modified after 'Sort()' is called, the list produced by 'ToList()' need no longer be sorted.
        self.table.sort(Sorter)