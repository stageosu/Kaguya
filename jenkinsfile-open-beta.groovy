pipeline {
    agent any
    stages {
        stage('Build') {
            steps {
                sh "docker build -t kaguya:${BUILD_NUMBER} ."
                sh "docker tag kaguya:${BUILD_NUMBER} kaguya:latest"

                sh "docker build -t kaguya-migrate:${BUILD_NUMBER} -f Migrations.Dockerfile ."
                sh "docker tag kaguya-migrate:${BUILD_NUMBER} kaguya-migrate:latest"
            }
        }
        stage('Stop') {
            steps {
                catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
                    sh "docker stop kaguya"
                    sh "docker rm kaguya"
                }

                catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
                    sh "docker rm kaguya-migrate"
                }
            }
        }
        stage('Pre-Deployment') {
            steps {
                sh "docker run --name=kaguya-migrate -v /etc/kaguya/appsettings.json:/Kaguya/appsettings.json kaguya-migrate:latest"
            }
        }
        stage('Deploy') {
            steps {
                sh "docker build -t kaguya:${BUILD_NUMBER} ."
                sh "docker tag kaguya:${BUILD_NUMBER} kaguya:latest"
                sh "docker run --name=kaguya --restart=always -p 6969:5000 -v /etc/kaguya/appsettings.json:/KaguyaApp/appsettings.json -d kaguya:latest"
            }
        }
    }
}