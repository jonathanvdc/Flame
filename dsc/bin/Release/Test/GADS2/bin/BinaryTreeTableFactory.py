from TreeTable import *
from BinarySearchTree import *
from IFactory import *

class BinaryTreeTableFactory(IFactory):
    """ A factory that creates empty binary search tree tables from a value-key mapping function. """

    def __init__(self):
        pass

    def create(self, Argument):
        """ Creates a new instance from the provided argument. """
        # Post:
        # Creates a new instance of type 'T', and returns it.
        return TreeTable(BinarySearchTree(Argument))