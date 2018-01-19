const superb = require('superb')

module.exports = {
  prompts: {
    name: {
      message: 'What is the name of your azure fn?',
      default: ':folderName:'
    },
    description: {
      message: 'What is the Description?',
      default: `My Mighty Azure Function ^_^`
    },
    username: {
      message: 'What is your Git username?',
      default: ':gitUser:',
      store: true
    },
    email: {
      message: 'What is your Git email?',
      default: ':gitEmail:',
      store: true
    },
    accountname: {
      message: 'What is a value for ACCOUNT_NAME?',
      default: 'inhabitetl'
    },
    resourcegroup: {
      message: 'What is a value for RESOURCE_GROUP?',
      default: 'inhabit-etl'
    }
  },
  move: {
    'gitignore': '.gitignore'
  },
  showTip: true,
  gitInit: true,
  installDependencies: true
}
