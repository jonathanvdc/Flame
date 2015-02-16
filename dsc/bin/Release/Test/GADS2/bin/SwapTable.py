from IndirectTable import *

class SwapTable(IndirectTable):
    """ A wrapper table that allows for the underlying table to be swapped out for another table. """

    def __init__(self, table):
        """ Creates a new instance of a swap table. """
        IndirectTable.__init__(self)
        self.table = table

    def get_table(self):
        """ Gets the indirect table's underlying table.
            This method is protected. """
        return self.table

    def swap(self, Table):
        """ Changes the underlying table implementation to the provided table. """
        # Post:
        # The underlying implementation of this table will be changed to 'Table', which will be populated with the items from the previous underlying table, in addition to the elements that were already in 'Table'.
        for item in self:
            Table.insert(item)
        self.table = Table